using System;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Migrations
{
    public static class MigrationExtensions
    {
        private static Lazy<IServiceProvider> _serviceProvider;
        /// <summary>
        /// Run all available migrations forward
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        public static void RunMigrationsUp(string connectionString)
        {

            GetServiceProvider(connectionString).GetRequiredService<IMigrationRunner>().MigrateUp();
        }

        public static void RollbackMigration(string connectionString)
        {
            GetServiceProvider(connectionString).GetRequiredService<IMigrationRunner>().Rollback(1);
        }

        private static IServiceProvider GetServiceProvider(string connectionString)
        {
            _serviceProvider = new Lazy<IServiceProvider>(() =>
            {
                return new ServiceCollection()
                    .AddFluentMigratorCore()
                    .ConfigureRunner(rb => rb
                        .AddPostgres()
                        .WithGlobalConnectionString(connectionString)
                        .ScanIn(typeof(MigrationExtensions).Assembly).For.Migrations()
                        .ScanIn(typeof(MigrationExtensions).Assembly).For.EmbeddedResources())
                    .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Trace))
                    .Configure<FluentMigratorLoggerOptions>(options =>
                    {
                        options.ShowSql = true;
                        options.ShowElapsedTime = true;
                    })
                    .BuildServiceProvider(false);
            });

            return _serviceProvider.Value;

        }
    }
}
