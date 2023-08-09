using EventService.Application.Persistence;
using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using EventService.Application.Exceptions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EventService.Infrastructure.Repositories
{
    public class EventRepository : RepositoryBase<Event>, IEventRepository
    {
        private readonly IEventInvitationRepository _eventInvitationRepository;
        private readonly IEventNotificationRepository _eventNotificationRepository;

        public EventRepository(EventContext dbContext,
            IEventInvitationRepository eventInvitationRepository, IEventNotificationRepository eventNotificationRepository)
            : base(dbContext)
        {
            _eventInvitationRepository = eventInvitationRepository
                ?? throw new ArgumentNullException(nameof(eventInvitationRepository));
            _eventNotificationRepository = eventNotificationRepository
                ?? throw new ArgumentNullException(nameof(eventNotificationRepository));
        }

        public async Task<IEnumerable<Event>> GetEvents(string userId)
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

        public async Task UpdateEvent(Event mappedEntity)
        {
            var eventEntity = await GetEvent(mappedEntity.EventId);

            // distinct by invitees and notifications only
            mappedEntity.Invitees = mappedEntity.Invitees.GroupBy(car => car.InviteeEmailId)
              .Select(g => g.First())
              .ToList();
            mappedEntity.Notifications = mappedEntity.Notifications.GroupBy(car => car.NotificationDate)
              .Select(g => g.First())
            .ToList();

            eventEntity.StartDate = mappedEntity.StartDate;
            eventEntity.EndDate = mappedEntity.EndDate;
            eventEntity.Location = mappedEntity.Location;
            eventEntity.Description = mappedEntity.Description;
            eventEntity.Timezone = mappedEntity.Timezone;
            eventEntity.Name = mappedEntity.Name;
            eventEntity.LastModifiedBy = mappedEntity.LastModifiedBy;


            var taskList = new List<Task>();
            var notificationsToAdd = mappedEntity.Notifications
                .ExceptBy(eventEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();
            var notificationsToRemove = eventEntity.Notifications
                .ExceptBy(mappedEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();

            using var transaction = _dbContext.Database.BeginTransaction();

            try
            {
                foreach (var item in notificationsToAdd)
                {
                    item.EventId = eventEntity.Id;
                    item.CreatedBy = mappedEntity.LastModifiedBy;
                    item.CreatedDate = DateTime.UtcNow;
                    taskList.Add(_eventNotificationRepository.AddAsync(item));
                }

                foreach (var item in notificationsToRemove)
                {
                    taskList.Add(_eventNotificationRepository.DeleteAsync(item));
                }

                var inviteesToAdd = mappedEntity.Invitees
                    .ExceptBy(eventEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();
                var inviteesToRemove = eventEntity.Invitees
                    .ExceptBy(mappedEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();

                foreach (var item in inviteesToAdd)
                {
                    item.EventId = eventEntity.Id;
                    item.CreatedBy = mappedEntity.LastModifiedBy;
                    item.CreatedDate = DateTime.UtcNow;
                    taskList.Add(_eventInvitationRepository.AddAsync(item));
                }

                foreach (var item in inviteesToRemove)
                {
                    taskList.Add(_eventInvitationRepository.DeleteAsync(item));
                }

                await Task.WhenAll(taskList).ContinueWith(async _ => await UpdateAsync(eventEntity)).Unwrap();

                transaction.Commit();
            }
            catch (DbUpdateException dbEx)
            {
                transaction.Rollback();
                throw new DatabaseException(dbEx.Message, dbEx.InnerException);
            }
        }
    }
}
