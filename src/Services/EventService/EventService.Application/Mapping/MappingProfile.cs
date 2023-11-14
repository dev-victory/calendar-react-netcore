using EventService.Domain.Entities;
using AutoMapper;
using EventService.Application.Models;
using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Features.Events.Commands.UpdateEvent;

namespace EventService.Application.Mapping
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            CreateMap<Event, EventVm>().ReverseMap();
            CreateMap<Event, CreateEventCommand>().ReverseMap();
            CreateMap<Event, UpdateEventCommand>().ReverseMap();
            CreateMap<EventInvitation, EventInvitationVm>().ReverseMap();
            CreateMap<EventNotification, EventNotificationVm>().ReverseMap();
        }
    }
}
