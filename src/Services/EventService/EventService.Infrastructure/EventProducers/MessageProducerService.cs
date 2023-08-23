using Confluent.Kafka;
using EventBus.Message.Constants;
using EventBus.Message.Messages;
using EventService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Infrastructure.EventProducers
{
    public class MessageProducerService : IMessageProducerService
    {
        private readonly ProducerConfig _producerConfig;
        private readonly ILogger<MessageProducerService> _logger;

        public MessageProducerService(IConfiguration config, ILogger<MessageProducerService> logger)
        {
            _logger = logger;
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = config.GetSection("Kafka:ProducerSettings:Server").Value
            };
        }

        public async Task SendNewEventMessage(NewCalendarEventMessage message)
        {
            using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();

            try
            {
                var response = await producer.ProduceAsync(Topics.NEW_EVENT_TOPIC, new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(message)
                });

                producer.Flush();

#if DEBUG
                _logger.LogInformation($"Event {message.Name} for invitee {message.InviteeEmail} succesfully sent to queue {Topics.NEW_EVENT_TOPIC}");
#endif
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex.Message);
                producer.Dispose();
                throw;
            }
        }
    }
}
