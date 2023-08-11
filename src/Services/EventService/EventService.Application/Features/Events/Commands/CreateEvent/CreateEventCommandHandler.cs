using AutoMapper;
using EventBus.Message.Messages;
using EventService.Application.Exceptions;
using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IMessageProducerService _messageProducerService;
        private readonly ILogger<CreateEventCommandHandler> _logger;
        private readonly IDistributedCache _redisCache;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IMapper mapper,
            IMessageProducerService messageProducerService,
            ILogger<CreateEventCommandHandler> logger,
            IDistributedCache redisCache)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageProducerService = messageProducerService ?? throw new ArgumentNullException(nameof(messageProducerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var newEvent = new Event();

            try
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


                newEvent = await _eventRepository.AddAsync(eventEntity);
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

                // Publish message to event bus for notifications
                if (hasInvitees)
                {
                    message.Invitees = newEvent.Invitees.Select(x => x.InviteeEmailId).ToList();
                    await _messageProducerService.SendNewEventMessage(message);
                }

                // update cache
                var cache = await _redisCache.GetStringAsync(newEvent.CreatedBy, cancellationToken);
                if (cache != null)
                {
                    var cachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);
                    cachedEvents.Add(newEvent);
                    await _redisCache.SetStringAsync(newEvent.CreatedBy,
                        JsonSerializer.Serialize(cachedEvents),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                        });
                }
            }
            catch (DatabaseException dbEx)
            {
                _logger.LogError($"Error: Event could not be created in the database, details: \n{dbEx.Message}");
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
                _logger.LogError($"Error: Event could not be created, details: \n{ex.Message}");
                throw new InternalErrorException((int)ServerErrorCodes.Unknown, "Something went wrong");
            }

            return newEvent.EventId;
        }
    }
}
