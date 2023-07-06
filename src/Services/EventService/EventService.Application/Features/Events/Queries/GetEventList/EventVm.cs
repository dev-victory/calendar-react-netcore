using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class EventVm
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public EventStatus Status { get; set; }
        public string Timezone { get; set; }
        public ICollection<EventInvitationVm> Invitees { get; set; } = new List<EventInvitationVm>();
        public ICollection<EventNotificationVm> Notifications { get; set; } = new List<EventNotificationVm>();
    }
}
