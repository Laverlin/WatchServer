using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;


namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helper class for Linq 2 DB configuration
    /// </summary>
    public class Linq2DBSettings : ILinqToDBSettings
    {
        private readonly IConnectionStringSettings _connectionStringSettings;
        public Linq2DBSettings(IConnectionStringSettings connectionStringSettings)
        {
            _connectionStringSettings = connectionStringSettings;
        }

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();
        public string DefaultConfiguration => _connectionStringSettings.Name;
        public string DefaultDataProvider => _connectionStringSettings.ProviderName;

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get { yield return _connectionStringSettings; }
        }
    }
}
