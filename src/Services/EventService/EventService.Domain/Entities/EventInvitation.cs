using EventService.Domain.Common;
using System.Text.Json.Serialization;

namespace EventService.Domain.Entities
{
    public class EventInvitation : EntityBase
    {
        public int EventId { get; set; }
        public string InviteeEmailId { get; set; }

        [JsonIgnore]
        public Event Event { get; set; } = null!;
    }
}
