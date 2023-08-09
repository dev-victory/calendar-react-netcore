using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateEventCommandHandler> _logger;

        public UpdateEventCommandHandler(
            IEventRepository eventRepository,
            IEventInvitationRepository eventInvitationRepository,
            IEventNotificationRepository eventNotificationRepository,
            IMapper mapper,
            ILogger<UpdateEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            var mappedEntity = _mapper.Map<Event>(request);

            try
            {
                await _eventRepository.UpdateEvent(mappedEntity);
            }
            catch (DatabaseException dbEx)
            {
                _logger.LogError($"Event {request.EventId} could not be updated, details: \n{dbEx.Message}");
            }

            _logger.LogInformation($"Event {request.EventId} is successfully updated");
        }
    }
}
