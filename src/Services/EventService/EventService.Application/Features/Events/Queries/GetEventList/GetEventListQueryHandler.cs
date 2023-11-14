using AutoMapper;
using EventService.Application.Constants;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Utilities;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQueryHandler : IRequestHandler<GetEventListQuery, List<EventVm>>
    {
        private const string EmptyRedisCacheString = "[]";
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _redisCache;
        private readonly ILogger<GetEventListQueryHandler> _logger;
        private readonly int _cacheExpiryInMinutes;

        public GetEventListQueryHandler(IEventRepository eventRepository,
            IMapper mapper,
            IDistributedCache redisCache,
            ILogger<GetEventListQueryHandler> logger,
            IOptions<RedisSettings> redisSettings)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheExpiryInMinutes = redisSettings.Value.CacheExpiryInMinutes;
        }

        public async Task<List<EventVm>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyList<Event> eventList = new List<Event>();

            try
            {
                if (request.IsFilterByWeek)
                {
                    var cache = await _redisCache.GetStringAsync(request.UserId, cancellationToken);
                    // get results from DB and set cache, if no results returned by cache
                    if (string.IsNullOrEmpty(cache) || cache == EmptyRedisCacheString)
                    {
                        eventList = await _eventRepository.GetEvents(request.UserId);
                        await _redisCache.SetStringAsync(
                            request.UserId,
                            JsonSerializer.Serialize(eventList),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_cacheExpiryInMinutes)
                            });

                        return ResetEventDatesToLocalTime(
                            _mapper.Map<List<EventVm>>(eventList.Where(e => !e.IsDeleted)));
                    }

                    var mappedCachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);

                    return ResetEventDatesToLocalTime(
                        _mapper.Map<List<EventVm>>(mappedCachedEvents.Where(e => !e.IsDeleted)));
                }

                eventList = await _eventRepository.GetEvents(request.UserId, request.IsFilterByWeek);
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
                _logger.LogError(string.Format(DomainErrors.EventFetchError, request.UserId, ex.Message));
                throw new InternalErrorException((int)ServerErrorCodes.Unknown, DomainErrors.SomethingWentWrong);
            }

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
