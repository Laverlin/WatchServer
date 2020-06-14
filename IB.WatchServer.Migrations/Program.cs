using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Configuration;

namespace IB.WatchServer.Migrations
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddCommandLine(args)
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .Build();

            var postgresConnectionSettings = config.LoadVerifiedConfiguration<PostgresProviderSettings>();
            var msSqlConnectionSettings = config.LoadVerifiedConfiguration<MsSqlProviderSettings>();

            var pgFactory = new DataConnectionFactory(postgresConnectionSettings);
            var msSqlFactory = new DataConnectionFactory(msSqlConnectionSettings);

            await Postgres2MsSql(pgFactory , msSqlFactory);
        }


        public async static Task Postgres2MsSql(DataConnectionFactory pgFactory, DataConnectionFactory msSqlFactory)
        {
            await using var pgConnection = pgFactory.Create();
            await using var msSqlConnection = msSqlFactory.Create();

            msSqlConnection.Command.CommandText = "SET IDENTITY_INSERT DeviceInfo ON";
            msSqlConnection.Command.ExecuteNonQuery();

            int count = 0;
            var devices = await pgConnection.GetTable<DeviceData>().ToArrayAsync();
            foreach (var device in devices)
            {
                count++;
                var requests = await pgConnection.GetTable<RequestData>()
                    .Where(r => r.DeviceDataId == device.Id)
                    .ToArrayAsync();
                
                await msSqlConnection.GetTable<DeviceData>().DataContext.InsertAsync(device);
                msSqlConnection.GetTable<RequestData>().BulkCopy(requests);

                Console.Write("\r{0} out of {1} devices processed - {2}% :: current: {3}, requests:{4}                           ", 
                    count, devices.Length, count * 100 / devices.Length, device.DeviceName, requests.Length);
            }
        }
    }
}
