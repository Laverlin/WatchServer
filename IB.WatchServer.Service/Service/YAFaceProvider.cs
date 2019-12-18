using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;

using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using LinqToDB.DataProvider.PostgreSQL;

namespace IB.WatchServer.Service.Service
{
    /// <summary>
    /// Provider for the Watch Face services
    /// </summary>
    public class YAFaceProvider
    {
        private readonly PostgresSettings _postgresSettings;

        public YAFaceProvider(PostgresSettings postgresSettings)
        {
            _postgresSettings = postgresSettings;
        }

        /// <summary>
        /// Get count of unique devices fixed in DB
        /// </summary>
        public async Task<long> GetDeviceCount()
        {
            using var db = new DataConnection(new PostgreSQLDataProvider(), _postgresSettings.ConnectionString);
            return await db.GetTable<DeviceInfo>().CountAsync();
        }
    }
}
