using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace IB.WatchServer.Service.Infrastructure.Linq2DB
{
    /// <summary>
    /// Factory to work with Data Connection from DI 
    /// </summary>
    public class DataConnectionFactory
    {
        private readonly IDataProvider _dataProvider;
        private readonly string _connectionString;

        /// <summary>
        /// Store parameters for Create
        /// </summary>
        /// <param name="dataProvider">Data provider entity</param>
        /// <param name="connectionString">Connection string</param>
        public DataConnectionFactory(IDataProvider dataProvider, string connectionString)
        {
            _dataProvider = dataProvider;
            _connectionString = connectionString;
        }

        public DataConnectionFactory(IConnectionSettings connectionSettings)
        {
            _dataProvider = connectionSettings.GetDataProvider();
            _connectionString = connectionSettings.GetConnectionString();
        }

        /// <summary>
        /// Create data connection with provided settings
        /// </summary>
        /// <returns><see cref="DataConnection"/></returns>
        public DataConnection Create()
        {
            return new DataConnection(_dataProvider, _connectionString);
        }
    }        
}
