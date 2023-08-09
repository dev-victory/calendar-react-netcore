namespace EventBus.Message.Messages
{
    public class NewCalendarEventMessage
    {
        public Guid EventId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ICollection<string> Invitees { get; set; }
    }
}
