using System;
using LinqToDB.DataProvider;
using IB.WatchServer.Service.Entity.Settings;
using LinqToDB.Common;
using LinqToDB.Data;

namespace IB.WatchServer.Service.Infrastructure
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

        public DataConnectionFactory(IConnectionSettings connectionSettings) :
            this(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
        { }

        public virtual DataConnection Create()
        {
            if (_dataProvider == null) throw new ArgumentNullException(nameof(_dataProvider));
            if (_connectionString.IsNullOrEmpty()) throw new ArgumentNullException(nameof(_connectionString));

            return new DataConnection(_dataProvider, _connectionString);
        }
    }
}
