using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Repositories
{
    public class EventRepository : RepositoryBase<Event>, IEventRepository
    {
        public EventRepository(EventContext dbContext) : base(dbContext)
        {
            
        }

        public async Task<IEnumerable<Event>> GetEvents(Guid userId)
        {
            var eventList = await _dbContext.Events
                .Where(x=> x.CreatedBy == userId)
                .ToListAsync();

            return eventList;
        }
    }
}
