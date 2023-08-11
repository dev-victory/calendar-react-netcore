using EventService.Application.Exceptions;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EventService.Infrastructure.Repositories
{
    public class EventRepository : RepositoryBase<Event>, IEventRepository
    {
        private readonly IEventInvitationRepository _eventInvitationRepository;
        private readonly IEventNotificationRepository _eventNotificationRepository;

        public EventRepository(EventContext dbContext,
            IEventInvitationRepository eventInvitationRepository, 
            IEventNotificationRepository eventNotificationRepository)
            : base(dbContext)
        {
            _eventInvitationRepository = eventInvitationRepository
                ?? throw new ArgumentNullException(nameof(eventInvitationRepository));
            _eventNotificationRepository = eventNotificationRepository
                ?? throw new ArgumentNullException(nameof(eventNotificationRepository));
        }

        public async Task<IReadOnlyList<Event>> GetEvents(string userId, bool isFilterByWeek = true)
        {
            var _Type = typeof(Event);

            var _Prop = _Type.GetProperty("CreatedBy");
            var _Param = Expression.Parameter(_Type, _Prop.Name);
            var _Left = Expression.PropertyOrField(_Param, _Prop.Name);
            var _Right = Expression.Constant(userId, _Prop.PropertyType);

            // Create an expression for the first condition: CreatedBy == userId
            var _Body1 = Expression.Equal(_Left, _Right);

            // Get the current date
            var currentDate = DateTime.UtcNow.Date;

            // Get the start date for the second condition: StartDate >= currentDate - 3 days
            var startDate = currentDate.AddDays(-3);

            // Get the end date for the third condition: EndDate <= currentDate + 3 days
            var endDate = currentDate.AddDays(3);

            // Create expressions for the second and third conditions using the same parameter
            var _Body2 = Expression.GreaterThanOrEqual(Expression.PropertyOrField(_Param, "StartDate"), Expression.Constant(startDate));
            var _Body3 = Expression.LessThanOrEqual(Expression.PropertyOrField(_Param, "EndDate"), Expression.Constant(endDate));

            // Combine the expressions using AndAlso if isFilterByWeek is true, otherwise use only the first expression
            var _Body = isFilterByWeek ? Expression.AndAlso(Expression.AndAlso(_Body1, _Body2), _Body3) : _Body1;

            // Create the final lambda expression
            var _Where = Expression.Lambda<Func<Event, bool>>(_Body, _Param);

            // Call the GetAsync method with the expression
            var eventList = await GetAsync(_Where);

            return eventList;
        }


        // TODO: warn: Microsoft.EntityFrameworkCore.Query[20504]
        //Compiling a query which loads related collections for more than one collection navigation,
        //either via 'Include' or through projection, but no 'QuerySplittingBehavior' has been configured.
        //By default, Entity Framework will use 'QuerySplittingBehavior.SingleQuery',
        //which can potentially result in slow query performance.See https://go.microsoft.com/fwlink/?linkid=2134277
        //for more information.
        //To identify the query that's triggering this warning call
        //'ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))'
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

        public async Task<Event> UpdateEvent(Event mappedEntity, Event dbEntity)
        {
            using var transaction = _dbContext.Database.BeginTransaction();

            try
            {
                dbEntity.StartDate = mappedEntity.StartDate;
                dbEntity.EndDate = mappedEntity.EndDate;
                dbEntity.Location = mappedEntity.Location;
                dbEntity.Description = mappedEntity.Description;
                dbEntity.Timezone = mappedEntity.Timezone;
                dbEntity.Name = mappedEntity.Name;
                dbEntity.LastModifiedBy = mappedEntity.LastModifiedBy;


                var taskList = new List<Task>();
                var notificationsToAdd = mappedEntity.Notifications
                    .ExceptBy(dbEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();
                var notificationsToRemove = dbEntity.Notifications
                    .ExceptBy(mappedEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();

                foreach (var item in notificationsToAdd)
                {
                    item.EventId = dbEntity.Id;
                    item.CreatedBy = mappedEntity.LastModifiedBy;
                    item.CreatedDate = DateTime.UtcNow;
                    taskList.Add(_eventNotificationRepository.AddAsync(item));
                }

                foreach (var item in notificationsToRemove)
                {
                    taskList.Add(_eventNotificationRepository.DeleteAsync(item));
                }

                var inviteesToAdd = mappedEntity.Invitees
                    .ExceptBy(dbEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();
                var inviteesToRemove = dbEntity.Invitees
                    .ExceptBy(mappedEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();

                foreach (var item in inviteesToAdd)
                {
                    item.EventId = dbEntity.Id;
                    item.CreatedBy = mappedEntity.LastModifiedBy;
                    item.CreatedDate = DateTime.UtcNow;
                    taskList.Add(_eventInvitationRepository.AddAsync(item));
                }

                foreach (var item in inviteesToRemove)
                {
                    taskList.Add(_eventInvitationRepository.DeleteAsync(item));
                }

                await Task.WhenAll(taskList).ContinueWith(async _ => await UpdateAsync(dbEntity)).Unwrap();

                
                transaction.Commit();

                return dbEntity;
            }
            catch (DbUpdateException dbEx)
            {
                transaction.Rollback();
                throw new DatabaseException(dbEx.Message, dbEx.InnerException);
            }
        }
    }
}
