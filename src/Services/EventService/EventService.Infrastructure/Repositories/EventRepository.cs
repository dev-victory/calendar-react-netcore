using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EventService.Infrastructure.Repositories
{
    public class EventRepository : RepositoryBase<Event>, IEventRepository
    {
        public EventRepository(EventContext dbContext) : base(dbContext)
        {
            
        }

        public async Task<IEnumerable<Event>> GetEvents(Guid userId)
        {
            var _Type = typeof(Event);
            var _Prop = _Type.GetProperty("CreatedBy");
            var _Param = Expression.Parameter(_Type, _Prop.Name);
            var _Left = Expression.PropertyOrField(_Param, _Prop.Name);
            var _Right = Expression.Constant(userId, _Prop.PropertyType);
            var _Body = Expression.Equal(_Left, _Right);
            var _Where = Expression.Lambda<Func<Event, bool>>(_Body, _Param);

            var eventList = await GetAsync(_Where);

            return eventList;
        }
    }
}
