using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Utilities;
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
                    _logger.LogWarning($"Forbidden: User {request.UserId} doesn't have access to event ID: {request.EventId}");
                    throw new ForbiddenAccessException();
                }

                eventDetails.StartDate = eventDetails.StartDate.ToLocalDate(eventDetails.Timezone);
                eventDetails.EndDate = eventDetails.EndDate.ToLocalDate(eventDetails.Timezone);

                foreach (var notification in eventDetails.Notifications)
                {
                    notification.NotificationDate = notification.NotificationDate.ToLocalDate(eventDetails.Timezone);
                }
            }

            return _mapper.Map<EventVm>(eventDetails);
        }
    }
}
