using System;
using System.Collections.Generic;
using System.Text;

namespace IB.WatchServer.RequestCollector
{
    public class CollectorSettings
    {
        public string SqlConnection { get; set; }
        
        /// <summary>
        /// Kafka Server Url and port
        /// </summary>
        public string KafkaServer { get; set; }

        /// <summary>
        /// Queue topic 
        /// </summary>
        public string KafkaTopic { get; set; }

        /// <summary>
        /// Consumer group
        /// </summary>
        public string KafkaConsumerGroup { get; set; }
    }
}
