using AutoMapper;
using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteEventCommandHandler> _logger;

        public DeleteEventCommandHandler(IEventRepository eventRepository, IMapper mapper, ILogger<DeleteEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = await _eventRepository.GetEvent(request.EventId);

            // TODO: isDeleted flag?

            await _eventRepository.DeleteAsync(eventEntity);
            _logger.LogInformation($"Event {eventEntity.EventId} is deleted");
        }
    }
}
