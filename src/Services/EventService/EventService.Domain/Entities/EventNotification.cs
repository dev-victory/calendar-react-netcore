using EventService.Domain.Common;

namespace EventService.Domain.Entities
{
    public class EventNotification : EntityBase
    {
        public DateTime NotificationDate { get; set; }
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;
    }
}
