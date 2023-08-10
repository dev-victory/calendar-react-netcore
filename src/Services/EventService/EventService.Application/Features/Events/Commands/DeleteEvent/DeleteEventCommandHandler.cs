using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<DeleteEventCommandHandler> _logger;
        private readonly IDistributedCache _redisCache;

        public DeleteEventCommandHandler(
            IEventRepository eventRepository,
            ILogger<DeleteEventCommandHandler> logger,
            IDistributedCache redisCache)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        }

        public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = await _eventRepository.GetEvent(request.EventId);
            eventEntity.IsDeleted = true;

            await _eventRepository.UpdateAsync(eventEntity);

            // update cache
            var cache = await _redisCache.GetStringAsync(eventEntity.CreatedBy, cancellationToken);
            if (cache != null)
            {
                var cachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);
                var cachedEventItem = cachedEvents.FirstOrDefault(e => e.EventId == eventEntity.EventId);
                if (cachedEventItem != null)
                {
                    cachedEvents.Remove(cachedEventItem);
                    await _redisCache.SetStringAsync(eventEntity.CreatedBy,
                        JsonSerializer.Serialize(cachedEvents),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                        });
                }
            }

            _logger.LogInformation($"Event {eventEntity.EventId} is deleted");
        }
    }
}
