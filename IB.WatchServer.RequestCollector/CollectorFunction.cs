using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.RequestCollector
{
    /// <summary>
    /// Azure Function to get data from message stream and put it to DB
    /// </summary>
    public class CollectorFunction
    {
        private readonly CollectorSettings _collectorSettings;

        public CollectorFunction(CollectorSettings collectorSettings)
        {
            _collectorSettings = collectorSettings;
        }

        [FunctionName("CollectorFunction")]
        public void Run([TimerTrigger("*/5 * * * * *")]TimerInfo timerInfo, ILogger logger, ExecutionContext context)
        {
            logger.LogDebug($"C# Timer trigger function executed at: {DateTime.Now}");
            logger.LogInformation($"Value SQL: {_collectorSettings.SqlConnection}");
            logger.LogInformation($"Value Kafka: {_collectorSettings.KafkaConnection}");
        }
    }
}
