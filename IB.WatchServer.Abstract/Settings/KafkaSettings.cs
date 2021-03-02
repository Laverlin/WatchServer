using System.ComponentModel.DataAnnotations;

namespace IB.WatchServer.Abstract.Settings
{
    public class KafkaSettings
    {
        /// <summary>
        /// Kafka Server Url and port
        /// </summary>
        [Required]
        public string KafkaServer { get; set; }

        public string UserName {get; set;}

        public string Password { get; set; }

        /// <summary>
        /// Queue topic 
        /// </summary>
        [Required]
        public string KafkaTopic { get; set; }

        /// <summary>
        /// Consumer group
        /// </summary>
        [Required]
        public string KafkaConsumerGroup { get; set; }
    }
}
