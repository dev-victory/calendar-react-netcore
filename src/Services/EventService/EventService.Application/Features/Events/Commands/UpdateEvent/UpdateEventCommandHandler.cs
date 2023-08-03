using AutoMapper;
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

        public UpdateEventCommandHandler(IEventRepository eventRepository, IMapper mapper, ILogger<UpdateEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = _mapper.Map<Event>(request);
            eventEntity.LastModifiedBy = request.ModifiedBy;

            // TODO: handle invitee and notification deletion and addition
            // notifications: remove all and add all again? coz there can only be 4 notifications as of now
            // invitees: match up against the incoming list and delete not-required ones

            await _eventRepository.UpdateAsync(eventEntity);
            _logger.LogInformation($"Event {eventEntity.EventId} is successfully updated");
        }
    }
}
