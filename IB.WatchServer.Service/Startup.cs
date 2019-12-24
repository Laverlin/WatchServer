using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Infrastructure.Linq2DB;
using IB.WatchServer.Service.Service;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using System.Linq;

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
            // configuration
            //
            services.AddConfiguration<FaceSettings>();
            services.AddConfiguration<IConnectionSettings, PostgresProviderSettings>();

            // services
            //
            services.AddHttpClient();
            services.AddSingleton<DataConnectionFactory>();
            services.AddScoped<YAFaceProvider>();
            services.AddScoped<RequestRateLimit>();

            services.AddControllers();

            services.AddHostedService<StartupHostedService>();

            // metrics
            //
            var metrics = AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .Build();

            services.AddMetrics(metrics);

            services.AddMetricsTrackingMiddleware();
            services.AddMetricsReportingHostedService();
            services.AddMetricsEndpoints(options => options.MetricsEndpointOutputFormatter =
                metrics.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First());

            // for AppMetric prometheus endpoint
            //
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMetricsAllMiddleware();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseAppMetricsEndpointRoutesResolver();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseMetricsEndpoint();

        }
    }
}
