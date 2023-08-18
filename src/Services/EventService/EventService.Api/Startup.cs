using Confluent.Kafka;
using EventService.Api.Authorization;
using EventService.Api.Settings;
using EventService.Application;
using EventService.Application.Filters;
using EventService.Application.Models;
using EventService.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

namespace EventService.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices(Configuration);

            #region Auth
            var authenticationProviderScheme = "Bearer";
            var authSettings = Configuration.GetSection("Auth").Get<AuthSettings>();

            services.AddAuthentication()
               .AddJwtBearer(authenticationProviderScheme, x =>
               {
                   x.Authority = authSettings.Authority; // TODO move to appsettings.json
                   x.Audience = authSettings.Audience;
               });


            services.AddScoped<IAuthorizationHandler, RoleLevelHandler>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("MustBeVerifiedUser", policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.AddRequirements(new RoleLevelRequirement("User"));
                });
            });
            #endregion

            services.AddOptions();
            services.Configure<RedisSettings>(Configuration.GetSection("Redis"));

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(CustomExceptionFilter));
            });

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
