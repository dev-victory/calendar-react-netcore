using Confluent.Kafka;
using EventBus.Message.Constants;
using EventBus.Message.Messages;
using NotificationService.Email;
using System.Text.Json;

namespace NotificationService.EventConsumers
{
    public class MessageConsumerService
    {
        private readonly ConsumerConfig _config;
        private readonly IEmailService _emailService;

        public MessageConsumerService(IConfiguration config, IEmailService emailService)
        {
            _config = new ConsumerConfig
            {
                GroupId = config.GetSection("Kafka:ConsumerSettings:GroupId").Value,
                BootstrapServers = config.GetSection("Kafka:ConsumerSettings:Server").Value,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _emailService = emailService;
        }

        public async Task FetchNewEventMessage()
        {
            using var consumer = new ConsumerBuilder<Null, string>(_config).Build();
            CancellationTokenSource token = new();
            consumer.Subscribe(Topics.NEW_EVENT_TOPIC);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent the process from terminating.
                token.Cancel();
            };

            try
            {
                while (true)
                {
                    var response = consumer.Consume(token.Token);
                    if (response.Message != null)
                    {
                        var message = JsonSerializer.Deserialize<NewCalendarEventMessage>(response.Message.Value);
                        Console.WriteLine($"Event {message.Name} succesfully fetched from queue {Topics.NEW_EVENT_TOPIC}");

                        var invitees = message.Invitees;
                        if (invitees != null && invitees.Count > 0)
                        {
                            foreach (var invitee in invitees)
                            {
                                var email = new EmailMessage
                                {
                                    Body = $"You are invited to an event, details: {message.Description}."
                                           + $"{Environment.NewLine}The event starts on " 
                                           + $"{message.StartDate.ToString("dddd, dd MMMM yyyy")} at "
                                           + $"{message.StartDate.ToString("hh:mm tt")}",
                                    Subject = $"You have been invited to {message.Name}",
                                    To = invitee
                                };

                                // TODO: Create send grid account and pass a valid API Key
                                await _emailService.SendEmail(email);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                consumer.Close();
                Console.WriteLine(ex.Message); // TODO: Logging
                throw;
            }
        }
    }
}
