using AutoMapper;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
    public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventInvitationRepository _eventInvitationRepository;
        private readonly IEventNotificationRepository _eventNotificationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateEventCommandHandler> _logger;

        public UpdateEventCommandHandler(
            IEventRepository eventRepository,
            IEventInvitationRepository eventInvitationRepository,
            IEventNotificationRepository eventNotificationRepository,
            IMapper mapper,
            ILogger<UpdateEventCommandHandler> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _eventInvitationRepository = eventInvitationRepository ?? throw new ArgumentNullException(nameof(eventInvitationRepository));
            _eventNotificationRepository = eventNotificationRepository ?? throw new ArgumentNullException(nameof(eventNotificationRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = await _eventRepository.GetEvent(request.EventId);
            var mappedEntity = _mapper.Map<Event>(request);

            // distinct by invitees and notifications only
            mappedEntity.Invitees = mappedEntity.Invitees.GroupBy(car => car.InviteeEmailId)
              .Select(g => g.First())
              .ToList();
            mappedEntity.Notifications = mappedEntity.Notifications.GroupBy(car => car.NotificationDate)
              .Select(g => g.First())
              .ToList();

            eventEntity.StartDate = request.StartDate;
            eventEntity.EndDate = request.EndDate;
            eventEntity.Location = request.Location;
            eventEntity.Description = request.Description;
            eventEntity.Timezone = request.Timezone;
            eventEntity.Name = request.Name;
            eventEntity.LastModifiedBy = request.ModifiedBy;


            var taskList = new List<Task>();
            var notificationsToAdd = mappedEntity.Notifications.ExceptBy(eventEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();
            var notificationsToRemove = eventEntity.Notifications.ExceptBy(mappedEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();

            foreach (var item in notificationsToAdd)
            {
                item.EventId = eventEntity.Id;
                item.CreatedBy = request.ModifiedBy;
                item.CreatedDate = DateTime.UtcNow;
                taskList.Add(_eventNotificationRepository.AddAsync(item));
            }

            foreach (var item in notificationsToRemove)
            {
                taskList.Add(_eventNotificationRepository.DeleteAsync(item));
            }

            var inviteesToAdd = mappedEntity.Invitees.ExceptBy(eventEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();
            var inviteesToRemove = eventEntity.Invitees.ExceptBy(mappedEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();

            foreach (var item in inviteesToAdd)
            {
                item.EventId = eventEntity.Id;
                item.CreatedBy = request.ModifiedBy;
                item.CreatedDate = DateTime.UtcNow;
                taskList.Add(_eventInvitationRepository.AddAsync(item));
            }

            foreach (var item in inviteesToRemove)
            {
                taskList.Add(_eventInvitationRepository.DeleteAsync(item));
            }

            await Task.WhenAll(taskList).ContinueWith(_ => _eventRepository.UpdateAsync(eventEntity)).Unwrap();

            _logger.LogInformation($"Event {eventEntity.EventId} is successfully updated");
        }
    }
}
