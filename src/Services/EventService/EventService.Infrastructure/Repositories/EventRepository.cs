using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
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

        public async Task<Event> GetEvent(Guid eventId)
        {
            var _Type = typeof(Event);
            var _Prop = _Type.GetProperty("EventId");
            var _Param = Expression.Parameter(_Type, _Prop.Name);
            var _Left = Expression.PropertyOrField(_Param, _Prop.Name);
            var _Right = Expression.Constant(eventId, _Prop.PropertyType);
            var _Body = Expression.Equal(_Left, _Right);
            var _Where = Expression.Lambda<Func<Event, bool>>(_Body, _Param);

            // eager loading child entities
            var includes = new List<Expression<Func<Event, object>>>
            {
                c => c.Notifications,
                c => c.Invitees
            };

            var eventList = await GetAsync(_Where, includes: includes);

            var eventDetails = new Event();
            
            // TODO find a better way to execute this child relation data
            if (eventList.Any())
            {
                eventDetails = eventList.FirstOrDefault();
            }

            return eventDetails;
        }
    }
}
