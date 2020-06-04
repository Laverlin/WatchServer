using System;
using System.Diagnostics.CodeAnalysis;
using LinqToDB.DataProvider;
using IB.WatchServer.Abstract.Settings;
using LinqToDB.Common;
using LinqToDB.Data;

namespace IB.WatchServer.Abstract
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
        public DataConnectionFactory([NotNull] IDataProvider dataProvider, [NotNull] string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) throw new ArgumentNullException(nameof(connectionString));

            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _connectionString = connectionString;
        }

        public DataConnectionFactory(IConnectionSettings connectionSettings) :
            this(connectionSettings.GetDataProvider(), connectionSettings.BuildConnectionString())
        { }

        public virtual DataConnection Create()
        {
            return new DataConnection(_dataProvider, _connectionString);
        }
    }
}
