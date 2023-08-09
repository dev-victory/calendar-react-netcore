using EventService.Domain.Entities;

namespace EventService.Application.Persistence
{
    public interface IEventRepository : IAsyncRepository<Event>
    {
        Task<IEnumerable<Event>> GetEvents(string userId);
        Task<Event> GetEvent(Guid eventId);
        Task UpdateEvent(Event mappedEvent);
    }
}
