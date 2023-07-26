namespace EventService.Application.Models
{
    public class EventVm : BaseEventVm
    {
        public Guid EventId { get; set; }
        public string CreatedBy { get; set; }
    }
}
