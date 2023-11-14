using EventService.Domain.Entities;

namespace EventService.Application.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventModel
    {
        public Event Event { get; set; }
        public List<EventNotification> AddNotifications { get; set; }
        public List<EventNotification> RemoveNotifications { get; set; }
        public List<EventInvitation> AddInvitees { get; set; }
        public List<EventInvitation> RemoveInvitees { get; set; }
    }
}
