using EventService.Domain.Common;

namespace EventService.Domain.Entities
{
    public class Event : EntityBase
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public EventStatus Status { get; set; }
        public string Timezone { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<EventInvitation> Invitees { get; set; } = new List<EventInvitation>();
        public ICollection<EventNotification> Notifications { get; set; } = new List<EventNotification>();
    }
}
