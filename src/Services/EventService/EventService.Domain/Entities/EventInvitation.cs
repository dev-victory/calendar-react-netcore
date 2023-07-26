using EventService.Domain.Common;

namespace EventService.Domain.Entities
{
    public class EventInvitation : EntityBase
    {
        public int EventId { get; set; }
        public string InviteeEmailId { get; set; }
        public Event Event { get; set; } = null!;
    }
}
