namespace EventService.Application.Services
{
    public interface IMessageProducerService<T>
    {
        Task SendNewEventMessage(T message, string topic);
    }
}
