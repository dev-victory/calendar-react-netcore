using Confluent.Kafka;
using EventBus.Message.Constants;
using EventBus.Message.Messages;
using EventService.Application.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace EventService.Infrastructure.EventProducers
{
    public class MessageProducerService : IMessageProducerService
    {
        private readonly ProducerConfig _producerConfig;

        public MessageProducerService(IConfiguration config)
        {
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
                // TODO: handle notifications using a new topic

                var response = await producer.ProduceAsync(Topics.NEW_EVENT_TOPIC, new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(message)
                });

                Console.WriteLine($"Event {message.Name} succesfully sent to queue {Topics.NEW_EVENT_TOPIC}");
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine(ex.Message); // TODO: Logging
                throw;
            }
        }
    }
}
