using EventService.Domain.Entities;

namespace EventService.Application.Persistence
{
    public interface IEventInvitationRepository : IAsyncRepository<EventInvitation>
    {
    }
}
