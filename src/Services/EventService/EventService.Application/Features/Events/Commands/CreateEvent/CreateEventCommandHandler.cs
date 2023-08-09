using AutoMapper;
using EventBus.Message.Messages;
using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IMessageProducerService _messageProducerService;
        private readonly ILogger<CreateEventCommandHandler> _logger;

        public CreateEventCommandHandler(
            IEventRepository eventRepository, 
            IMapper mapper, 
            IMessageProducerService messageProducerService, 
            ILogger<CreateEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageProducerService = messageProducerService ?? throw new ArgumentNullException(nameof(messageProducerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = _mapper.Map<Event>(request);
            eventEntity.EventId = Guid.NewGuid();
            eventEntity.CreatedBy = request.CreatedBy;
            
            if (eventEntity.Notifications.Any())
            {
                foreach (var notification in eventEntity.Notifications)
                {
                    notification.CreatedBy = request.CreatedBy;
                }
            }

            var hasInvitees = eventEntity.Invitees.Any();
            if (hasInvitees)
            {
                foreach (var invitee in eventEntity.Invitees)
                {
                    invitee.CreatedBy = request.CreatedBy;
                }
            }

            var newEvent = await _eventRepository.AddAsync(eventEntity);
            _logger.LogInformation($"Event {newEvent.EventId} is successfully created");

            // Send message to Kafka about the new event
            var message = new NewCalendarEventMessage
            {
                Name = newEvent.Name,
                EventId = newEvent.EventId,
                Description = newEvent.Description,
                EndDate = newEvent.EndDate,
                StartDate = newEvent.StartDate
            };

            if (hasInvitees)
            {
                message.Invitees = newEvent.Invitees.Select(x=> x.InviteeEmailId).ToList();
                await _messageProducerService.SendNewEventMessage(message);
            }

            return newEvent.EventId;
        }
    }
}
