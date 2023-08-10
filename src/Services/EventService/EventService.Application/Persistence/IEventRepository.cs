using EventService.Domain.Entities;

namespace EventService.Application.Persistence
{
    public interface IEventRepository : IAsyncRepository<Event>
    {
        Task<IReadOnlyList<Event>> GetEvents(string userId, bool isFilterByWeek = true);
        Task<Event> GetEvent(Guid eventId);
        Task<Event> UpdateEvent(Event mappedEvent);
    }
}
