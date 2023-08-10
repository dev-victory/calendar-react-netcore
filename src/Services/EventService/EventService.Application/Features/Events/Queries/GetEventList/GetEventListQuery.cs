using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class GetEventListQuery : IRequest<List<EventVm>>
    {
        public string UserId { get; set; }
        public bool IsFilterByWeek { get; set; }

        public GetEventListQuery(string userId, bool isGetByMonth)
        {
            UserId = userId;
            IsFilterByWeek = isGetByMonth;
        }
    }
}
