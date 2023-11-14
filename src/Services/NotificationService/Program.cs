using NotificationService.Email;
using NotificationService.EventConsumers;

namespace NotificationService
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.Configure<EmailSettings>(c => builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<NewEventMessageConsumerService>();
            var app = builder.Build();

            var logger = app.Services.GetService<ILogger<NewEventMessageConsumerService>>();

            app.MapGet("/", async () =>
            {
                var service = new NewEventMessageConsumerService(builder.Configuration, app.Services.GetRequiredService<IEmailService>(), logger);
                logger.LogInformation("Connecting to Kafka event bus...");
                await service.FetchNewEventMessage();
                return "Exiting Notification Service...";
            });

            app.Run();
        }
    }
}