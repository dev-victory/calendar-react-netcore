using AutoMapper;
using EventBus.Message.Constants;
using EventBus.Message.Messages;
using EventService.Application.Constants;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Application.Utilities;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IMessageProducerService<NewCalendarEventMessage> _messageProducerService;
        private readonly ILogger<CreateEventCommandHandler> _logger;
        private readonly IDistributedCache _redisCache;
        private readonly int _cacheExpiryInMinutes;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IMapper mapper,
            IMessageProducerService<NewCalendarEventMessage> messageProducerService,
            ILogger<CreateEventCommandHandler> logger,
            IDistributedCache redisCache,
            IOptions<RedisSettings> redisSettings)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageProducerService = messageProducerService ?? throw new ArgumentNullException(nameof(messageProducerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _cacheExpiryInMinutes = redisSettings.Value.CacheExpiryInMinutes;
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var newEvent = new Event();

            try
            {
                var eventEntity = _mapper.Map<Event>(request);
                eventEntity.EventId = Guid.NewGuid();
                eventEntity.CreatedBy = request.CreatedBy;
                eventEntity.StartDate = request.StartDate != DateTime.MinValue ? request.StartDate.ToUtcDate(request.Timezone) : DateTime.Now;
                eventEntity.EndDate = request.EndDate != DateTime.MinValue ? request.EndDate.ToUtcDate(request.Timezone) : DateTime.Now;


                if (eventEntity.Notifications?.Any() ?? false)
                {
                    foreach (var notification in eventEntity.Notifications)
                    {
                        notification.CreatedBy = request.CreatedBy;
                        notification.NotificationDate = notification.NotificationDate.ToUtcDate(request.Timezone);
                    }
                }

                var hasInvitees = eventEntity.Invitees?.Count > 0;
                if (hasInvitees)
                {
                    foreach (var invitee in eventEntity.Invitees)
                    {
                        invitee.CreatedBy = request.CreatedBy;
                    }
                }

                newEvent = await _eventRepository.AddAsync(eventEntity);

                _logger.LogInformation($"Event {newEvent.EventId} is successfully created");

#if DEBUG
                _logger.LogInformation($"Sending notifications to the event queue");
#endif

                await SendEventNotifications(newEvent, hasInvitees);

                _logger.LogInformation($"Notifications sent successfully to the event queue named \"{Topics.NEW_EVENT_TOPIC}\"");

                await UpdateCache(newEvent, cancellationToken);
            }
            catch (DatabaseException dbEx)
            {
                _logger.LogError(string.Format(DomainErrors.EventModifyDatabaseError, Guid.Empty, "created", dbEx.Message));

                throw new InternalErrorException((int)ServerErrorCodes.DatabaseError, DomainErrors.SomethingWentWrong);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(string.Format(DomainErrors.RedisCacheTimeout, ex.Message));
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(string.Format(DomainErrors.RedisCacheConnectionError, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format(DomainErrors.EventModifyError, Guid.Empty, "created", ex.Message));

                throw new InternalErrorException((int)ServerErrorCodes.Unknown, DomainErrors.SomethingWentWrong);
            }

            return newEvent.EventId;
        }

        private async Task SendEventNotifications(Event newEvent, bool hasInvitees)
        {
            // Send message to Kafka about the new event
            var message = new NewCalendarEventMessage
            {
                Name = newEvent.Name,
                EventId = newEvent.EventId,
                Description = newEvent.Description,
                EndDate = newEvent.EndDate,
                StartDate = newEvent.StartDate,
                Timezone = newEvent.Timezone
            };

            // Publish message to event queue for event create notification to invitees
            // TODO: handle notifications queue using a different topic
            if (hasInvitees)
            {
                foreach (var invitee in newEvent.Invitees)
                {
                    message.InviteeEmail = invitee.InviteeEmailId;
                    await _messageProducerService.SendNewEventMessage(message, Topics.NEW_EVENT_TOPIC);
                }
            }
        }

        private async Task UpdateCache(Event newEvent, CancellationToken cancellationToken)
        {
            var cacheData = await _redisCache.GetStringAsync(newEvent.CreatedBy, cancellationToken);
            if (!string.IsNullOrEmpty(cacheData))
            {
                var cachedEvents = JsonSerializer.Deserialize<List<Event>>(cacheData);
                cachedEvents.Add(newEvent);
                await _redisCache.SetStringAsync(newEvent.CreatedBy,
                    JsonSerializer.Serialize(cachedEvents),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_cacheExpiryInMinutes)
                    });
            }
        }
    }
}
