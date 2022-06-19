using IB.WatchServer.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace IB.WatchServer.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
             Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => { config.AddEnvironmentVariables().Build(); })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                { 
                    loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.WithProperty("version", SolutionInfo.Version)
                        .Enrich.WithProperty("Application", SolutionInfo.Name);

                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.WithProperty("version", SolutionInfo.Version)
                        .Enrich.WithProperty("Application", SolutionInfo.Name)
                        .CreateLogger();
                    Log.Logger.Information("Initializing");
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
