using Confluent.Kafka;
using EventService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Infrastructure.EventProducers
{
    public class MessageProducerService<T> : IMessageProducerService<T>
    {
        private readonly ProducerConfig _producerConfig;
        private readonly ILogger<MessageProducerService<T>> _logger;

        public MessageProducerService(IConfiguration config, ILogger<MessageProducerService<T>> logger)
        {
            _logger = logger;
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = config.GetSection("Kafka:ProducerSettings:Server").Value
            };
        }

        public async Task SendNewEventMessage(T message, string topic)
        {
            using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();

            try
            {
                var response = await producer.ProduceAsync(topic, new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(message)
                });

                producer.Flush();

#if DEBUG
                _logger.LogInformation($"Event succesfully sent to queue {topic}");
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
