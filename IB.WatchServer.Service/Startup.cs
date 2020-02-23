using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.Prometheus;
using AutoMapper;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Telegram.Bot;
using Serilog.Context;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using IB.WatchServer.Service.Entity.Settings;

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
            services.AddSingleton(faceSettings);
            services.AddSingleton(pgSettings);

            // services
            //
            services.AddSingleton<DataConnectionFactory>();
            services.AddScoped<YAFaceProvider>();
            services.AddScoped<RequestRateLimit>();

            services.AddControllers();

            services.AddHostedService<StartupHostedService>();

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

            // HttpClient
            //
            services.AddHttpClient(Options.DefaultName)
                .AddPolicyHandler((serviceProvider, request) => HttpPolicyExtensions.HandleTransientHttpError()
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt * 3),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<YAFaceProvider>();
                            logger.LogWarning("Delaying for {delay}ms, then making retry {retry}. CorrelationId {correlationId}",
                                timespan.TotalMilliseconds, retryAttempt, context.CorrelationId);
                        }
                    ));

            // AutoMapper Configuration
            //
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.CreateMap<WatchFaceRequest, RequestInfo>()
                    .ForMember(d => d.Lat, c=> c.MapFrom(s => Convert.ToDecimal(s.Lat)))
                    .ForMember(d => d.Lon, c=> c.MapFrom(s => Convert.ToDecimal(s.Lon)));
                mc.CreateMap<WeatherResponse, RequestInfo>();
                mc.CreateMap<Dictionary<string, object>, WeatherResponse>()
                    .ForMember(d => d.Temperature, c => c.MapFrom(s => s.ContainsKey("temp") ? s["temp"] : 0))
                    .ForMember(d => d.WindSpeed, c => c.MapFrom(s => s.ContainsKey("speed") ? s["speed"] : 0))
                    .ForMember(d => d.Humidity,
                        c => c.MapFrom(s => s.ContainsKey("humidity") ? Convert.ToDecimal(s["humidity"]) / 100 : 0))
                    .ForAllOtherMembers(o => o.MapFrom(s =>
                        s.ContainsKey(o.DestinationMember.Name.ToLower())
                            ? s[o.DestinationMember.Name.ToLower()]
                            : null));
            });
            services.AddSingleton(mappingConfig.CreateMapper());

            // for AppMetric prometheus endpoint
            //
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // Add telegram bot
            //
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(faceSettings.TelegramKey));
            services.AddSingleton<TelegramService>();

            // Add the health check
            //
            services.AddHealthChecks()
                .AddNpgSql(pgSettings.BuildConnectionString(), name: "database")
                .AddUrlGroup(faceSettings.BuildLocationUrl("0", "0"), "location")
                .AddUrlGroup(new Uri("https://api.darksky.net/v1/status.txt"), "darkSky")
                .AddUrlGroup(faceSettings.BuildOpenWeatherUrl("0", "0"), "openWeather");
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
                LogContext.PushProperty("UserName", context.User.Identity.Name);
                LogContext.PushProperty("Headers", context.Request.Headers.ToDictionary(h => h.Key, h=>h.Value.ToString()));
                await next.Invoke();
            });

            app.UseAppMetricsEndpointRoutesResolver();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions {ResponseWriter = WriteHealthResultResponse});
            });
            app.UseMetricsEndpoint();
        }

        /// <summary>
        /// Custom JSON output for HealthReport
        /// </summary>
        private static Task WriteHealthResultResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions
            {
                Indented = true
            };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                writer.WriteString("serverVersion", SolutionInfo.Version);
                writer.WriteString("status", result.Status.ToString());
                writer.WriteString("totalDuration", result.TotalDuration.ToString());
                writer.WriteStartObject("results");
                foreach (var entry in result.Entries)
                {
                    writer.WriteStartObject(entry.Key);
                    writer.WriteString("status", entry.Value.Status.ToString());
                    writer.WriteString("description", entry.Value.Description);
                    writer.WriteStartObject("data");
                    foreach (var item in entry.Value.Data)
                    {
                        writer.WritePropertyName(item.Key);
                        JsonSerializer.Serialize(writer, item.Value, item.Value?.GetType() ?? typeof(object));
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());

            return context.Response.WriteAsync(json);
        }
    }
}
