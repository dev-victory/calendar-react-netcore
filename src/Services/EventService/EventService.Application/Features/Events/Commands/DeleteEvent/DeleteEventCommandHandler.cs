using EventService.Application.Constants;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<DeleteEventCommandHandler> _logger;
        private readonly IDistributedCache _redisCache;
        private readonly int _cacheExpiryInMinutes;

        public DeleteEventCommandHandler(
            IEventRepository eventRepository,
            ILogger<DeleteEventCommandHandler> logger,
            IDistributedCache redisCache,
            IOptions<RedisSettings> redisSettings)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _cacheExpiryInMinutes = redisSettings.Value.CacheExpiryInMinutes;
        }

        public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var eventEntity = await _eventRepository.GetEvent(request.EventId);

                if (eventEntity.CreatedBy != request.UserId)
                {
                    _logger.LogWarning(string.Format(DomainErrors.EventUserForbiddenAccess, request.UserId, request.EventId));

                    throw new ForbiddenAccessException();
                }

                eventEntity.IsDeleted = true;

                await _eventRepository.UpdateAsync(eventEntity);
                await UpdateCache(eventEntity, cancellationToken);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(string.Format(DomainErrors.RedisCacheTimeout, ex.Message));
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(string.Format(DomainErrors.RedisCacheConnectionError, ex.Message));
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(string.Format(DomainErrors.EventModifyDatabaseError, request.EventId, "deleted", ex.Message));

                throw new InternalErrorException((int)ServerErrorCodes.Unknown, DomainErrors.SomethingWentWrong);
            }
            catch (ForbiddenAccessException forEx)
            {
                throw forEx;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format(DomainErrors.EventModifyError, request.EventId, "deleted", ex.Message));

                throw new InternalErrorException((int)ServerErrorCodes.Unknown, DomainErrors.SomethingWentWrong);
            }

            _logger.LogInformation($"Event {request.EventId} is deleted");
        }

        private async Task UpdateCache(Event eventEntity, CancellationToken cancellationToken)
        {
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
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_cacheExpiryInMinutes)
                        });
                }
            }
        }
    }
}
