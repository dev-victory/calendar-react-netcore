namespace EventService.Application.Models
{
    public class EventInvitationVm
    {
        public Guid InviterId { get; set; }
        public string InviteeEmailId { get; set; }
    }
}