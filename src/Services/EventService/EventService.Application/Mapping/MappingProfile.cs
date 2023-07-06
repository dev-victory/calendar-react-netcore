using EventService.Application.Features.Events.Queries.GetEventList;
using EventService.Domain.Entities;
using AutoMapper;

namespace EventService.Application.Mapping
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            CreateMap<Event, EventVm>().ReverseMap();
        }
    }
}
