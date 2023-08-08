using EventService.Domain.Common;
using System.Text.Json.Serialization;

namespace EventService.Domain.Entities
{
    public class EventNotification : EntityBase
    {
        public DateTime NotificationDate { get; set; }
        public int EventId { get; set; }

        [JsonIgnore]
        public Event Event { get; set; } = null!;
    }
}
