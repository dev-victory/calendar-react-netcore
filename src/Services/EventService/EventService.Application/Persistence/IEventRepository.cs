using EventService.Domain.Entities;

namespace EventService.Application.Persistence
{
    public interface IEventRepository : IAsyncRepository<Event>
    {
        Task<IEnumerable<Event>> GetEvents(Guid userId);
    }
}
