using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using IB.WatchServer.Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{
    public class KafkaProvider
    {
        private readonly KafkaSettings _kafkaSettings;
        private readonly ILogger<KafkaProvider> _logger;

        public KafkaProvider(KafkaSettings kafkaSettings, ILogger<KafkaProvider> logger)
        {
            _kafkaSettings = kafkaSettings;
            _logger = logger;
        }

        public virtual async Task SendMessage<TMessage>(TMessage message)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _kafkaSettings.KafkaServer,
                    ClientId = Dns.GetHostName(),
                    //EnableDeliveryReports = false,
                    MessageTimeoutMs = 10000
                };

                var payload = JsonSerializer.Serialize(message);

                using var producer = new ProducerBuilder<Null, string>(config).Build();
                await producer.ProduceAsync(_kafkaSettings.KafkaTopic, new Message<Null, string> {Value = payload});
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to send message");
            }

        }
    }
}
