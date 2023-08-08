using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;

namespace EventService.Infrastructure.Repositories
{
    public class EventNotificationRepository : RepositoryBase<EventNotification>, IEventNotificationRepository
    {
        public EventNotificationRepository(EventContext dbContext) : base(dbContext)
        {

        }
    }
}
