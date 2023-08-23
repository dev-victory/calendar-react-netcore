using Confluent.Kafka;
using EventBus.Message.Constants;
using EventBus.Message.Messages;
using NotificationService.Email;
using System.Text.Json;

namespace NotificationService.EventConsumers
{
    // TODO:
    // 1. secure access to Kafka using SASL
    //  reference - https://medium.com/tribalscale/kafka-security-configuring-sasl-authentication-on-net-core-apps-da5d0b0fcc5
    // 2. Create send grid account and pass a valid API Key
    public class MessageConsumerService
    {
        private readonly ConsumerConfig _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<MessageConsumerService> _logger;

        public MessageConsumerService(IConfiguration config, IEmailService emailService, ILogger<MessageConsumerService> logger)
        {
            _config = new ConsumerConfig
            {
                GroupId = config.GetSection("Kafka:ConsumerSettings:GroupId").Value,
                BootstrapServers = config.GetSection("Kafka:ConsumerSettings:Server").Value,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _emailService = emailService;
            _logger = logger;
        }

        public async Task FetchNewEventMessage()
        {
            CancellationTokenSource token = new();
            using var consumer = new ConsumerBuilder<Null, string>(_config).Build();

            try
            {
                consumer.Subscribe(Topics.NEW_EVENT_TOPIC);
                _logger.LogInformation($"Successfully connected and subscribed to {Topics.NEW_EVENT_TOPIC} topic");

                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    token.Cancel();
                };


                while (true)
                {
                    var response = consumer.Consume(token.Token);
                    if (response.Message != null)
                    {
                        var message = JsonSerializer.Deserialize<NewCalendarEventMessage>(response.Message.Value);
                        _logger.LogInformation($"Event {message.Name} for invitee {message.InviteeEmail} succesfully fetched from queue {Topics.NEW_EVENT_TOPIC}");

                        var email = new EmailMessage
                        {
                            // ideally the body will be rendered as HTML using dotliquid or others
                            Body = $"You are invited to an event, details: {message.Description}."
                                   + $"{Environment.NewLine}The event starts on "
                                   + $"{message.StartDate.ToString("dddd, dd MMMM yyyy")} at "
                                   + $"{message.StartDate.ToString("hh:mm tt")}",
                            Subject = $"You have been invited to {message.Name}",
                            To = message.InviteeEmail
                        };

                        await _emailService.SendEmail(email);
                    }
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Error while consuming messages from Kafka, details: \n{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred, details: \n{ex.Message}");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
