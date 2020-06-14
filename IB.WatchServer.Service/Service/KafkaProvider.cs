using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using IB.WatchServer.Abstract.Settings;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Service
{
    public class KafkaProvider
    {
        private readonly KafkaSettings _kafkaSettings;
        private readonly ILogger<KafkaProvider> _logger;
        private readonly ProducerConfig _producerConfig;

        public KafkaProvider(KafkaSettings kafkaSettings, ILogger<KafkaProvider> logger)
        {
            _kafkaSettings = kafkaSettings;
            _logger = logger;
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.KafkaServer,
                ClientId = Dns.GetHostName(),
                MessageTimeoutMs = 10000,

            };

            if (!_kafkaSettings.KafkaServer.StartsWith("localhost"))
            {
                _producerConfig.SaslMechanism = SaslMechanism.ScramSha256;
                _producerConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
                _producerConfig.SaslUsername = _kafkaSettings.UserName;
                _producerConfig.SaslPassword = _kafkaSettings.Password;
            }
        }

        public virtual async Task SendMessage<TMessage>(TMessage message)
        {
            try
            {
                var payload = JsonSerializer.Serialize(message);
                _logger.LogDebug("Writing to {kafkaServer}", _kafkaSettings.KafkaServer);

                using var producer = new ProducerBuilder<Null, string>(_producerConfig)
                    .SetLogHandler((_, logMessage) => _logger.Log(
                         (LogLevel)logMessage.LevelAs(LogLevelType.MicrosoftExtensionsLogging), logMessage.Message))
                    .Build();
                await producer.ProduceAsync(_kafkaSettings.KafkaTopic, new Message<Null, string> {Value = payload});
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to send message");
            }
        }
    }
}
