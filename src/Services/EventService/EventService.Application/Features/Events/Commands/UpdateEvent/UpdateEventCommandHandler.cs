using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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
            var mappedEntity = _mapper.Map<Event>(request);

            try
            {
                mappedEntity.LastModifiedBy = request.ModifiedBy;
                var updatedEvent = await _eventRepository.UpdateEvent(mappedEntity);

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
                _logger.LogError($"Event {request.EventId} could not be updated in the database, details: \n{dbEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Event {request.EventId} could not be updated, details: \n{ex.Message}");
            }

            _logger.LogInformation($"Event {request.EventId} is successfully updated");
        }
    }
}
