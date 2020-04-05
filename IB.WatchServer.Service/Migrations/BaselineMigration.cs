using FluentMigrator;

namespace IB.WatchServer.Service.Migrations
{
    [Migration(0, "Baseline migration")]
    public class BaselineMigration : Migration
    {
        public override void Up()
        {
            Execute.EmbeddedScript(@"IB.WatchServer.Service.Migrations.baseline-up.sql");
        }

        public override void Down()
        {
            Execute.EmbeddedScript(@"IB.WatchServer.Service.Migrations.baseline-down.sql");
        }
    }
}
