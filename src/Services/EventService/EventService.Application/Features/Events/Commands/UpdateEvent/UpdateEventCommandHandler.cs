using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Persistence;
using EventService.Application.Utilities;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateEventCommandHandler> _logger;
        private readonly IDistributedCache _redisCache;

        public UpdateEventCommandHandler(
            IEventRepository eventRepository,
            IMapper mapper,
            ILogger<UpdateEventCommandHandler> logger,
            IDistributedCache redisCache)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        }

        public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var mappedEntity = _mapper.Map<Event>(request);

                mappedEntity.LastModifiedBy = request.ModifiedBy;
                var dbEntity = await _eventRepository.GetEvent(request.EventId);

                // unique invitees and notifications only
                mappedEntity.Invitees = mappedEntity.Invitees.GroupBy(i => i.InviteeEmailId)
                    .Select(g => g.First()).ToList();
                mappedEntity.Notifications = mappedEntity.Notifications.GroupBy(n => n.NotificationDate)
                    .Select(g => g.First()).ToList();

                if (dbEntity != null)
                {
                    if (dbEntity.StartDate != request.StartDate)
                    {
                        mappedEntity.StartDate = request.StartDate.ToUtcDate(request.Timezone);
                    }
                    if (dbEntity.EndDate != request.EndDate)
                    {
                        mappedEntity.EndDate = request.EndDate.ToUtcDate(request.Timezone);
                    }

                    foreach (var notification in mappedEntity.Notifications)
                    {
                        var utcNotificationDate = notification.NotificationDate.ToUtcDate(mappedEntity.Timezone);
                        // match notification database date to the new date being added, if already exists don't reconvert to UTC
                        var notificationExists = dbEntity.Notifications.Any(n => n.NotificationDate == utcNotificationDate);
                        if (!notificationExists)
                        {
                            notification.NotificationDate = utcNotificationDate;
                        }
                    }
                }

                var updatedEvent = await _eventRepository.UpdateEvent(mappedEntity, dbEntity);

                // update cache
                var cache = await _redisCache.GetStringAsync(updatedEvent.CreatedBy, cancellationToken);
                if (cache != null)
                {
                    var cachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);
                    var cachedEventItem = cachedEvents.FirstOrDefault(e => e.EventId == request.EventId);
                    if (cachedEventItem != null)
                    {
                        cachedEvents.Remove(cachedEventItem);
                        cachedEvents.Add(updatedEvent);
                        await _redisCache.SetStringAsync(updatedEvent.CreatedBy,
                            JsonSerializer.Serialize(cachedEvents),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                            });
                    }
                }
            }
            catch (DatabaseException dbEx)
            {
                _logger.LogError($"Error: Event {request.EventId} could not be updated in the database, details: \n{dbEx.Message}");
                throw new InternalErrorException((int)ServerErrorCodes.DatabaseError, "Something went wrong");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError($"Connection to redis cache timed out, details: \n{ex.Message}");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError($"Error connecting to redis cache, details: \n{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: Event {request.EventId} could not be updated, details: \n{ex.Message}");
                throw new InternalErrorException((int)ServerErrorCodes.Unknown, "Something went wrong...");
            }

            _logger.LogInformation($"Event {request.EventId} is successfully updated");
        }
    }
}
