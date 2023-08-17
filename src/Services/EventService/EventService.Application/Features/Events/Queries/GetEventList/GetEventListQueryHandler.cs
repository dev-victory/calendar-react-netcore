using AutoMapper;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Utilities;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQueryHandler : IRequestHandler<GetEventListQuery, List<EventVm>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _redisCache;
        private readonly ILogger<GetEventListQueryHandler> _logger;

        public GetEventListQueryHandler(IEventRepository eventRepository,
            IMapper mapper,
            IDistributedCache redisCache,
            ILogger<GetEventListQueryHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<EventVm>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyList<Event> eventList = new List<Event>();

            try
            {
                if (request.IsFilterByWeek)
                {
                    var cache = await _redisCache.GetStringAsync(request.UserId, cancellationToken);
                    if (string.IsNullOrEmpty(cache))
                    {
                        eventList = await _eventRepository.GetEvents(request.UserId);
                        await _redisCache.SetStringAsync(
                            request.UserId,
                            JsonSerializer.Serialize(eventList),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                            });

                        return ResetEventDatesToLocalTime(
                            _mapper.Map<List<EventVm>>(eventList.Where(e => !e.IsDeleted)));
                    }

                    var mappedCachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);

                    return ResetEventDatesToLocalTime(
                        _mapper.Map<List<EventVm>>(mappedCachedEvents.Where(e => !e.IsDeleted)));
                }
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError($"Connection to redis cache timed out, details: \n{ex.Message}");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError($"Error connecting to redis cache, details: \n{ex.Message}");
            }

            eventList = await _eventRepository.GetEvents(request.UserId, request.IsFilterByWeek);

            return ResetEventDatesToLocalTime(
                _mapper.Map<List<EventVm>>(eventList.Where(e => !e.IsDeleted)));
        }

        private List<EventVm> ResetEventDatesToLocalTime(List<EventVm> events)
        {
            foreach (var e in events)
            {
                e.StartDate = e.StartDate.ToLocalDate(e.Timezone);
                e.EndDate = e.EndDate.ToLocalDate(e.Timezone);
            }

            return events;
        }
    }
}
