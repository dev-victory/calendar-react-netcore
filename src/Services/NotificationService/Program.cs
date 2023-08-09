using Microsoft.Extensions.Configuration;
using NotificationService.Email;
using NotificationService.EventConsumers;

namespace NotificationService
{
    public class Program
    {
        // TODO: configure startup boilerplate code with configs and DI for consumer!
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<EmailSettings>(c => builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<MessageConsumerService>();
            var app = builder.Build();

            app.MapGet("/", async () =>
            {
                var service = new MessageConsumerService(builder.Configuration, app.Services.GetRequiredService<IEmailService>());
                await service.FetchNewEventMessage();
                return "Hello from Notification Service!";
            });

            app.Run();
        }
    }
}