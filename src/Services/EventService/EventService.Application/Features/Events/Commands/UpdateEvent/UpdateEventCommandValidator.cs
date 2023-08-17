using EventService.Application.Features.Events.Commands.UpdateEvent;
using FluentValidation;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
    {
        public UpdateEventCommandValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("{Name} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{Name} must not exceed 50 characters.");

            RuleFor(p => p.StartDate)
                .NotEmpty().WithMessage("{StartDate} is required.");

            RuleFor(p => p.EndDate)
                .NotEmpty().WithMessage("{EndDate} is required.")
                .GreaterThan(r => r.StartDate).WithMessage("{EndDate} must be greater than {Start Date}.");

            RuleForEach(p => p.Invitees).SetValidator(i => new EventInvitationVmValidator());
        }
    }
}
