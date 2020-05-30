
using System;
using Confluent.Kafka;
using IB.WatchServer.Infrastructure.Settings;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.RequestCollector
{
    /// <summary>
    /// Azure Function to get data from message stream and put it to DB
    /// </summary>
    public class CollectorFunction
    {
        private readonly KafkaSettings _kafkaSettings;

        public CollectorFunction(KafkaSettings kafkaSettings)
        {
            _kafkaSettings = kafkaSettings;
        }

        /// <summary>
        /// Execute function
        /// </summary>
        /// <param name="timerInfo">timer info</param>
        /// <param name="logger">logger</param>
        /// <param name="context">context</param>
        [FunctionName("CollectorFunction")]
        public void Run([TimerTrigger("*/5 * * * * *")]TimerInfo timerInfo, ILogger logger, ExecutionContext context)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.KafkaServer,
                GroupId = _kafkaSettings.KafkaConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            consumer.Subscribe(_kafkaSettings.KafkaTopic);

            try
            {
                while (true)
                {
                    var payload = consumer.Consume();
                    if (payload.IsPartitionEOF)
                        break;

                    var message = payload?.Message.Value;
                    logger.LogInformation($"message: {message}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "consumer exception");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
