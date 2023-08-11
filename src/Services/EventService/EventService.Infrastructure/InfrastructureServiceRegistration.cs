using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Infrastructure.EventProducers;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Net;

namespace EventService.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EventContext>(options =>
                           options.UseSqlServer(configuration.GetConnectionString("EventConnectionString")));

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = async () =>
                {
                    var conn = configuration.GetSection("CacheSettings:ConnectionString").Value;

                    var configurationOptions = new ConfigurationOptions();
                    configurationOptions.EndPoints.Add(conn);
                    configurationOptions.ConnectTimeout = 10000;
                    configurationOptions.ConnectRetry = 3;

                    // Create and configure your ConnectionMultiplexer here
                    var connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
                    connection.ConnectionFailed += (sender, args) =>
                    {
                        // Handle connection failures here
                        Console.WriteLine($"Connection failed: {args.Exception}");
                    };
                    connection.ConnectionRestored += (sender, args) =>
                    {
                        // Handle connection recoveries here
                        Console.WriteLine($"Connection restored: {args.Exception}");
                    };

                    return connection;
                };
            });

            services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();
            services.AddScoped<IEventNotificationRepository, EventNotificationRepository>();
            services.AddSingleton<IMessageProducerService, MessageProducerService>();

            return services;
        }
    }
}
