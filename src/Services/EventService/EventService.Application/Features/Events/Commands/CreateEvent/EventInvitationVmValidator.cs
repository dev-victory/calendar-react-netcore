using EventService.Application.Models;
using FluentValidation;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class EventInvitationVmValidator : AbstractValidator<EventInvitationVm>
    {
        public EventInvitationVmValidator()
        {
            RuleFor(i => i.InviteeEmailId)
                .NotEmpty().WithMessage("{InviteeEmailId} is required")
                .EmailAddress().WithMessage("Please provide a valid email adddress");
        }
    }
}
