using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQuery : IRequest<List<EventVm>>
    {
        public Guid UserId { get; set; }

        public GetEventListQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
