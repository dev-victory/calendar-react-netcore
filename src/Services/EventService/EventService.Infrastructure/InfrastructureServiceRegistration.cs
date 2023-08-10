using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Infrastructure.EventProducers;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                options.Configuration = configuration.GetSection("CacheSettings:ConnectionString").Value;
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
