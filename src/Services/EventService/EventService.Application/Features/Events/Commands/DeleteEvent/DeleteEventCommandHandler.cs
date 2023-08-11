using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
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
            try
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
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError($"Connection to redis cache timed out, details: \n{ex.Message}");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError($"Error connecting to cache, details: \n{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: Event {request.EventId} could not be deleted, details: \n{ex.Message}");
                throw new InternalErrorException((int)ServerErrorCodes.Unknown, "Something went wrong...");
            }

            _logger.LogInformation($"Event {request.EventId} is deleted");
        }
    }
}
