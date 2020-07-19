using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Tools;
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
            string transferDateString = (string)config.GetValue(typeof(string), "transferDate");

            DateTime trDate = transferDateString.IsNullOrEmpty()
                ? DateTime.MaxValue
                : DateTime.Parse(transferDateString, new DateTimeFormatInfo {FullDateTimePattern = "YYYY-MM-DD"});


            await Postgres2MsSql(pgFactory , msSqlFactory, trDate);
        }


        public async static Task Postgres2MsSql(DataConnectionFactory pgFactory, DataConnectionFactory msSqlFactory, DateTime transferDate)
        {
            await using var pgConnection = pgFactory.Create();
            await using var msSqlConnection = msSqlFactory.Create();

           // msSqlConnection.Command.CommandText = "SET IDENTITY_INSERT DeviceInfo ON";
            //msSqlConnection.Command.ExecuteNonQuery();

            int count = 0;

            var devices = (transferDate == DateTime.MaxValue)
                ? await pgConnection.GetTable<DeviceData>().ToArrayAsync()
                : await pgConnection.GetTable<DeviceData>()
                    .Where(d=>d.Id.In(pgConnection.GetTable<RequestData>()
                        .Where(r=>r.RequestTime.Date == transferDate.Date).Select(r=>r.DeviceDataId)))
                    .ToArrayAsync();


            foreach (var device in devices)
            {
                count++;
                var requests = pgConnection.GetTable<RequestData>()
                    .Where(r => r.DeviceDataId == device.Id);

                    if (transferDate != DateTime.MaxValue)
                        requests = requests.Where(r => r.RequestTime.Date == transferDate.Date);
                    
                
                await msSqlConnection.GetTable<DeviceData>().DataContext.InsertOrReplaceAsync(device);
                msSqlConnection.GetTable<RequestData>().BulkCopy(
                    new BulkCopyOptions {KeepIdentity = true},
                    await requests.ToArrayAsync());

                Console.Write("\r{0} out of {1} devices processed - {2}% :: current: {3}, requests:{4}                           ", 
                    count, devices.Length, count * 100 / devices.Length, device.DeviceName, requests.Count());
            }
        }
    }
}
