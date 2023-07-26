using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQuery : IRequest<List<EventVm>>
    {
        public string UserId { get; set; }

        public GetEventListQuery(string userId)
        {
            UserId = userId;
        }
    }
}
