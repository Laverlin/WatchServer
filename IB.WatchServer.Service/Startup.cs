using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.Prometheus;
using IB.WatchServer.Abstract.Settings;
using IB.WatchServer.Abstract.Entity;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using IB.WatchServer.Service.Service.HttpClients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MihaZupan;
using Serilog;
using Serilog.Context;
using System;
using System.Linq;
using IB.WatchServer.Abstract;
using Telegram.Bot;

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
            var faceSettings = Configuration.LoadVerifiedConfiguration<FaceSettings>();
            var pgSettings = Configuration.LoadVerifiedConfiguration<IConnectionSettings, PostgresProviderSettings>();
            services.AddSingleton(Configuration.LoadVerifiedConfiguration<KafkaSettings>());
            services.AddSingleton(faceSettings);
            services.AddSingleton(pgSettings);

            // services
            //
            services.AddSingleton<DataConnectionFactory>();
            services.AddSingleton<KafkaProvider>();
            services.AddScoped<PostgresDataProvider>();
            services.AddScoped<ExchangeRateCacheStrategy>();
            services.AddScoped<RequestRateLimit>();

            services.AddControllers();

            services.AddHostedService<StartupHostedService>();

            services.AddApplicationInsightsTelemetry(Configuration);

            // metrics
            //
            var metrics = AppMetrics.CreateDefaultBuilder()
                .Configuration.Configure(
                    options => options.GlobalTags.Add("version", SolutionInfo.Version))
                .Configuration.ReadFrom(Configuration)
                .OutputMetrics.AsPrometheusPlainText()
                .Build();

            services.AddMetrics(metrics);

            services.AddMetricsTrackingMiddleware();
            services.AddMetricsReportingHostedService();
            services.AddMetricsEndpoints(
                options => options.MetricsEndpointOutputFormatter = metrics.OutputMetricsFormatters
                    .OfType<MetricsPrometheusTextOutputFormatter>().First());

            // Authentication
            //
            services
                .AddAuthentication(faceSettings.AuthSettings.Scheme)
                .AddScheme<TokenAuthOptions, TokenAuthenticationHandler>(
                    faceSettings.AuthSettings.Scheme, 
                    options =>
                    {
                        options.ApiTokenName = faceSettings.AuthSettings.TokenName;
                        options.Scheme = faceSettings.AuthSettings.Scheme;
                        options.ApiToken = faceSettings.AuthSettings.Token;
                    });
            services.AddAuthorization();

            // HttpClients
            //
            services.AddHttpClient<VirtualearthClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));
            services.AddHttpClient<CurrencyConverterClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));
            services.AddHttpClient<ExchangeRateApiClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));
            services.AddHttpClient<ExchangeRateHostClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));
            services.AddHttpClient<DarkSkyClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));
            services.AddHttpClient<OpenWeatherClient>().AddRetryPolicyWithCb(4, TimeSpan.FromMinutes(10));

            // AutoMapper Configuration
            //
            services.AddSingleton(MapperConfig.CreateMapper());

            // for AppMetric prometheus endpoint
            //
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // Add telegram bot
            //
            var proxy = (faceSettings.ProxySettings != null)
                ? new HttpToSocks5Proxy(faceSettings.ProxySettings.Host, faceSettings.ProxySettings.Port)
                : null;
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(faceSettings.TelegramKey, proxy));
            services.AddSingleton<TelegramService>();

            // Add the health check
            //
            services.AddHealthChecks()
                .AddNpgSql(pgSettings.BuildConnectionString(), name: "database")
                .AddUrlGroup(faceSettings.BuildLocationUrl(0, 0), "location")
                .AddUrlGroup(new Uri("https://api.darksky.net/v1/status.txt"), "darkSky")
                .AddUrlGroup(faceSettings.BuildOpenWeatherUrl(0, 0), "openWeather");

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ErrorResponses = new VersioningErrorProvider();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // ReSharper disable once UnusedMember.Global
        //
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSerilogRequestLogging();

            app.UseMetricsAllMiddleware();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add some request info to each log entry
            //
            app.Use(async (context, next) =>
            {
                LogContext.PushProperty("UserName", context.User.Identity?.Name);
                LogContext.PushProperty("Headers", context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));
                await next.Invoke();
            });

            app.UseAppMetricsEndpointRoutesResolver();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health",
                    new HealthCheckOptions {ResponseWriter = HealthCheckExtensions.WriteHealthResultResponse});
            });
            app.UseMetricsEndpoint();
        }

    }
}
