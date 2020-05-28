using System;
using System.Collections.Generic;
using System.Text;

namespace IB.WatchServer.RequestCollector
{
    public class CollectorSettings
    {
        public string SqlConnection { get; set; }
        public string KafkaConnection { get; set; }
    }
}
