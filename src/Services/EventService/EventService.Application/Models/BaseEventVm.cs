namespace EventService.Application.Models
{
    public class BaseEventVm
    {
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
