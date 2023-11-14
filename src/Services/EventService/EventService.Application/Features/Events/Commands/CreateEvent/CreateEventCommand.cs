using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommand : BaseEventVm, IRequest<Guid>
    {
        public string? CreatedBy { get; set; }
    }
}
