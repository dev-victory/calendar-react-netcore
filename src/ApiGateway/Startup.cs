using ApiGateway.Settings;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ApiGateway
{
    public class Startup
    {
        private const string CorsPolicyName = "AllowClient";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var authenticationProviderKey = "Bearer";
            var authSettings = Configuration.GetSection("Auth").Get<AuthSettings>();

            services.AddAuthentication()
            .AddJwtBearer(authenticationProviderKey, x =>
            {
                x.Authority = authSettings.Authority;
                x.Audience = authSettings.Audience;
            });

            services.AddOcelot()
                .AddCacheManager(settings => settings.WithDictionaryHandle());

            var corsSettings = Configuration.GetSection("Cors").Get<CorsSettings>();
            services.AddCors((options) => 
            {
                options.AddPolicy(CorsPolicyName, policy => 
                {
                    policy
                    .WithOrigins(new string[] { corsSettings.AllowedOrigins })
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Calendar Invite API gateway!");
                });
            });

            app.UseCors(CorsPolicyName);

            await app.UseOcelot();
        }
    }
}