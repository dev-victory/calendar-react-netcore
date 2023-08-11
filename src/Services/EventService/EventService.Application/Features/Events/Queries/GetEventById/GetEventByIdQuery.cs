using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQuery : IRequest<EventVm>
    {
        public Guid EventId { get; set; }
        public string UserId { get; set; }

        public GetEventByIdQuery(Guid eventId, string userId)
        {
            EventId = eventId;
            UserId = userId;
        }
    }
}
