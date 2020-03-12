using LinqToDB.DataProvider;

namespace IB.WatchServer.Service.Entity.Settings
{
    /// <summary>
    /// The contract for Connection providers
    /// </summary>
    public interface IConnectionSettings
    {
        /// <summary>
        /// The instance of <see cref="IDataProvider"/>
        /// </summary>
        /// <returns>The instance of <see cref="IDataProvider"/></returns>
        public IDataProvider GetDataProvider();
    }
}
