using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Migrations
{

    public class MigrationRunner
    {
        private readonly IMigrationRunner _migrationRunner;

        public MigrationRunner(string connectionString)
        {
            _migrationRunner = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(GetType().Assembly).For.Migrations()
                    .ScanIn(GetType().Assembly).For.EmbeddedResources())
                .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .Configure<FluentMigratorLoggerOptions>(options =>
                {
                    options.ShowSql = false;
                })
                .BuildServiceProvider(false)
                .GetRequiredService<IMigrationRunner>();
        }

        /// <summary>
        /// Run all available migrations forward
        /// </summary>
        public void RunMigrationsUp()
        {
            _migrationRunner.MigrateUp();
        }

        /// <summary>
        /// Down migration to the specific version
        /// </summary>
        /// <param name="migration">Specific migration to run down</param>
        public void RunMigrationDown(IMigration migration)
        {
            _migrationRunner.Down(migration);
        }
    }
}
