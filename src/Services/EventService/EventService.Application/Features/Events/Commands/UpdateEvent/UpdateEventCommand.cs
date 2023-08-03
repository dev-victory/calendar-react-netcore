using EventService.Application.Models;
using MediatR;

namespace EventService.Application.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventCommand : BaseEventVm, IRequest
    {
        public Guid EventId { get; set; }
        public string ModifiedBy { get; set; }
    }
}
