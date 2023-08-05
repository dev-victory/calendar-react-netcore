using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Application.Persistence;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery,EventVm>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetEventByIdQueryHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<EventVm> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var eventDetails = await _eventRepository.GetEvent(request.EventId);
            if (eventDetails == null || eventDetails.IsDeleted) 
            {
                throw new NotFoundException($"Event with Id {request.EventId} was not found");
            }

            return _mapper.Map<EventVm>(eventDetails);
        }
    }
}
