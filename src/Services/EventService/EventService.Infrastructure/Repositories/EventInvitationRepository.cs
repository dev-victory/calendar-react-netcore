using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;

namespace EventService.Infrastructure.Repositories
{
    public class EventInvitationRepository : RepositoryBase<EventInvitation>, IEventInvitationRepository
    {
        public EventInvitationRepository(EventContext dbContext) : base(dbContext)
        {

        }
    }
}
