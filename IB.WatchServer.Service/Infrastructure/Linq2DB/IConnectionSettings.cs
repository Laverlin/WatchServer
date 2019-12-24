using LinqToDB.DataProvider;

namespace IB.WatchServer.Service.Infrastructure.Linq2DB
{
    /// <summary>
    /// The contract for Connection providers
    /// </summary>
    public interface IConnectionSettings
    {
        /// <summary>
        /// Return connection string
        /// </summary>
        public string GetConnectionString();

        /// <summary>
        /// The instance of <see cref="IDataProvider"/>
        /// </summary>
        /// <returns>The instance of <see cref="IDataProvider"/></returns>
        public IDataProvider GetDataProvider();
    }
}
