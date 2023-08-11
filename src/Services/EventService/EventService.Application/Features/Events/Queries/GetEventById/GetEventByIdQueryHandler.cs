using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Application.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventVm>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetEventByIdQueryHandler> _logger;

        public GetEventByIdQueryHandler(IEventRepository eventRepository, IMapper mapper, ILogger<GetEventByIdQueryHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EventVm> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var eventDetails = await _eventRepository.GetEvent(request.EventId);
            if (eventDetails != null)
            {
                if (eventDetails.IsDeleted)
                {
                    throw new NotFoundException($"Event with Id {request.EventId} was not found");
                }

                if (eventDetails.CreatedBy != request.UserId)
                {
                    throw new UnauthorizedAccessException("You don't have permission to this event");
                }
            }

            return _mapper.Map<EventVm>(eventDetails);
        }
    }
}
