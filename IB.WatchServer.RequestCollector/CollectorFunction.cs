using System;
using System.Text.Json;
using Confluent.Kafka;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using LinqToDB;
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
        private readonly DataConnectionFactory _dbFactory;

        public CollectorFunction(KafkaSettings kafkaSettings, DataConnectionFactory dbFactory)
        {
            _kafkaSettings = kafkaSettings;
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Execute function
        /// </summary>
        /// <param name="timerInfo">timer info</param>
        /// <param name="logger">logger</param>
        /// <param name="context">context</param>
        [FunctionName("RequestConsumer")]
        public async void RequestConsumer([TimerTrigger("*/5 * * * * *")]TimerInfo timerInfo, ILogger logger, ExecutionContext context)
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

                    var message = payload.Message.Value;

                    var jsonMessage = JsonDocument.Parse(message);
                    var watchRequest = JsonSerializer.Deserialize<WatchRequest>(
                        jsonMessage.RootElement.GetProperty("watchRequest").GetRawText());
                    var weatherInfo = JsonSerializer.Deserialize<WeatherInfo>(
                        jsonMessage.RootElement.GetProperty("weatherInfo").GetRawText());
                    var locationInfo = JsonSerializer.Deserialize<LocationInfo>(
                        jsonMessage.RootElement.GetProperty("locationInfo").GetRawText());
                    var exchangeRateInfo = JsonSerializer.Deserialize<ExchangeRateInfo>(
                        jsonMessage.RootElement.GetProperty("exchangeRateInfo").GetRawText());

                    
                    
                    await using var dbConnection = _dbFactory.Create();
                    var deviceData = await dbConnection.GetTable<DeviceData>()
                        .SingleOrDefaultAsync(_ => _.DeviceId == watchRequest.DeviceId);
                    if (deviceData == null)
                    {
                        deviceData = new DeviceData
                        {
                            DeviceId = watchRequest.DeviceId,
                            DeviceName = watchRequest.DeviceName,
                            FirstRequestTime = watchRequest.RequestTime
                        };
                        deviceData.Id = await dbConnection.GetTable<DeviceData>().DataContext.InsertWithInt32IdentityAsync(deviceData);
                    }
                    
                    logger.LogInformation("id: {id}, request: {@WatchRequest}", payload.Offset.Value, watchRequest);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "consumer exception");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
