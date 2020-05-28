using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace IB.WatchServer.Service.Service
{
    public class KafkaProvider
    {
        public async Task SendMessage<TMessage>(TMessage message)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                ClientId = Dns.GetHostName(),
            };

            var payload = JsonSerializer.Serialize(message);

            using var producer = new ProducerBuilder<Null, string>(config).Build();
            await producer.ProduceAsync("test", new Message<Null, string> {Value = payload});
        }
    }
}
