using LinqToDB.Configuration;
using System.ComponentModel.DataAnnotations;


namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Postgress connection settings
    /// </summary>
    public class PostgresSettings : IConnectionStringSettings
    {
        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ProviderName { get; set; }

        public bool IsGlobal => false;
    }
}
