using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<DeleteEventCommandHandler> _logger;

        public DeleteEventCommandHandler(IEventRepository eventRepository, ILogger<DeleteEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = await _eventRepository.GetEvent(request.EventId);
            eventEntity.IsDeleted = true;

            await _eventRepository.UpdateAsync(eventEntity);

            _logger.LogInformation($"Event {eventEntity.EventId} is deleted");
        }
    }
}
