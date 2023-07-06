namespace EventService.Application.Features.Events.Queries.GetEventList
{
    public class EventInvitationVm
    {
        public Guid InviterId { get; set; }
        public int EventId { get; set; }
        public string InviteeEmailId { get; set; }
    }
}