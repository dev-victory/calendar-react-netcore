using EventService.Domain.Entities;

namespace EventService.Application.Persistence
{
    public interface IEventNotificationRepository : IAsyncRepository<EventNotification>
    {
    }
}
