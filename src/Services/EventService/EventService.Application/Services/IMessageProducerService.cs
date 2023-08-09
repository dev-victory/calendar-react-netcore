using EventBus.Message.Messages;

namespace EventService.Application.Services
{
    public interface IMessageProducerService
    {
        Task SendNewEventMessage(NewCalendarEventMessage calEvent);
    }
}
