using AutoMapper;
using EventService.Application.Persistence;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQueryHandler : IRequestHandler<GetEventListQuery, List<EventVm>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetEventListQueryHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<List<EventVm>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
        {
            var eventList = await _eventRepository.GetEvents(request.UserId);
            return _mapper.Map<List<EventVm>>(eventList);
        }
    }
}
