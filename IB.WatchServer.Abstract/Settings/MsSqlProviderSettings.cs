using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;

namespace IB.WatchServer.Abstract.Settings
{
    public class MsSqlProviderSettings : IConnectionSettings
    {
        /// <summary>
        /// Return new instance of <see cref="SqlServerDataProvider"/>
        /// </summary>
        public IDataProvider GetDataProvider()
        {
            var sqlProvider = SqlServerTools.GetDataProvider(SqlServerVersion.v2017);
            return sqlProvider;
        }

        /// <summary>
        /// Server name
        /// </summary>
        [Required]
        public string Server { get; set; }
        
        /// <summary>
        /// Database name
        /// </summary>
        [Required]
        public string Database { get; set; }

        /// <summary>
        /// Authorized user id
        /// </summary>
        [Required]
        [DisplayName("User Id")]
        public string UserId { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        [Required]
        public string Password { get; set; }
    }
}