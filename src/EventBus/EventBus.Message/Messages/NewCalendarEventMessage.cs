namespace EventBus.Message.Messages
{
    public class NewCalendarEventMessage
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Timezone { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string InviteeEmail { get; set; }
    }
}
