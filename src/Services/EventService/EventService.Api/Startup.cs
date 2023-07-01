using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using EventService.Infrastructure;

namespace EventService.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddApplicationServices();
            services.AddInfrastructureServices(Configuration);

            // MassTransit-RabbitMQ Configuration
            //services.AddMassTransit(config =>
            //{
            //    config.AddConsumer<BasketCheckoutConsumer>();
            //    config.UsingRabbitMq((ctx, cfg) =>
            //    {
            //        cfg.Host(Configuration["EventBusSettings:HostAddress"]);
            //        cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue, c =>
            //        {
            //            c.ConfigureConsumer<BasketCheckoutConsumer>(ctx);
            //        });
            //    });
            //});

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Event.API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Event.API v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
