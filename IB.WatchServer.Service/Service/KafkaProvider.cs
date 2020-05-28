using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{
    public class KafkaProvider
    {
        private readonly ILogger<KafkaProvider> _logger;

        public KafkaProvider(ILogger<KafkaProvider> logger)
        {
            _logger = logger;
        }

        public async Task SendMessage<TMessage>(TMessage message)
        {
            try
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
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to send message");
            }

        }
    }
}
