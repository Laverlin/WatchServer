using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using LinqToDB.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IB.WatchServer.Service
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
            services.AddSingleton(Configuration.GetSection(typeof(PostgresSettings).Name).Get<PostgresSettings>());
            services.AddSingleton(Configuration.GetSection(typeof(FaceSettings).Name).Get<FaceSettings>());

            services.AddHttpClient();
            services.AddScoped<YAFaceProvider>();
            services.AddHostedService<StartupHostedService>();
            services.AddScoped(factory => new RequestRateLimitAttribute 
            { 
                KeyField = "did",
                Seconds = 5,
                Logger = factory.GetRequiredService<ILogger<RequestRateLimitAttribute>>() 
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
