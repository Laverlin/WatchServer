using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Confluent.Kafka;
using IB.WatchServer.Abstract;
using IB.WatchServer.Abstract.Entity.WatchFace;
using IB.WatchServer.Abstract.Settings;
using LinqToDB;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace IB.WatchServer.RequestCollector
{
    /// <summary>
    /// Azure Function to get data from message stream and put it to DB
    /// </summary>
    public class CollectorFunction
    {
        private readonly KafkaSettings _kafkaSettings;
        private readonly DataConnectionFactory _dbFactory;
        private readonly IMapper _mapper;

        public CollectorFunction(KafkaSettings kafkaSettings, DataConnectionFactory dbFactory, IMapper mapper)
        {
            _kafkaSettings = kafkaSettings;
            _dbFactory = dbFactory;
            _mapper = mapper;
        }


        [FunctionName(nameof(KafkaPayload))]
        public async Task KafkaPayload([KafkaTrigger(
                "BootstrapServer",
                "8cxa2hx6-default",
                ConsumerGroup = "log-consumer",
                Protocol = BrokerProtocol.SaslSsl,
                AuthenticationMode = BrokerAuthenticationMode.ScramSha256,
                SslCaLocation = "KafkaCloudCert.pem",
                Username = "ConfluentCloudUsername",
                Password = "ConfluentCloudPassword")]
            KafkaEventData<string> kafkaEvent,
            ILogger logger)
        {
            logger.LogInformation(kafkaEvent.Value.ToString());

            var parsedPayload = ParsePayload(kafkaEvent.Value);

            await SaveData(
                parsedPayload.WatchRequest, parsedPayload.WeatherInfo, parsedPayload.LocationInfo, parsedPayload.ExchangeRateInfo);
        }

        /*
        /// <summary>
        /// Grab data from the queue and put them to persistent storage
        /// </summary>
        /// <param name="timerInfo">timer info</param>
        /// <param name="logger">logger</param>
        [FunctionName(nameof(ProcessPayload))]
        public async Task ProcessPayload([TimerTrigger("*5 * * * * *")]TimerInfo timerInfo, ILogger logger)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.KafkaServer,
                SaslMechanism = SaslMechanism.ScramSha256,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = _kafkaSettings.UserName,
                SaslPassword = _kafkaSettings.Password,
                GroupId = _kafkaSettings.KafkaConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true,
                EnableSslCertificateVerification = false
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe(_kafkaSettings.KafkaTopic);
            logger.LogInformation("subscribed on {topic}", _kafkaSettings.KafkaTopic);

            try
            {
                while (true)
                {
                    var payload = consumer.Consume();
                    if (payload.IsPartitionEOF)
                    {
                        logger.LogInformation("no record found");
                        break;
                    }

                    var parsedPayload = ParsePayload(payload);

                    await SaveData(
                        parsedPayload.WatchRequest, parsedPayload.WeatherInfo, parsedPayload.LocationInfo, parsedPayload.ExchangeRateInfo);
                    
                    logger.LogInformation("id: {id}, request: {@WatchRequest}", payload.Offset.Value, parsedPayload.WatchRequest);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Consumer exception");
            }
            finally
            {
                consumer.Close();
            }
        }*/

        /// <summary>
        /// Parse payload into typed objects 
        /// </summary>
        /// <param name="message">Queue message</param>
        /// <returns>Tuple with parsed objects</returns>
        private (WatchRequest WatchRequest, WeatherInfo WeatherInfo, LocationInfo LocationInfo, ExchangeRateInfo ExchangeRateInfo)
            ParsePayload(string message)//ConsumeResult<Ignore, string> payload)
        {
           // var message = payload.Message.Value;

            var jsonMessage = JsonDocument.Parse(message);
            var watchRequest = JsonSerializer.Deserialize<WatchRequest>(
                jsonMessage.RootElement.GetProperty("watchRequest").GetRawText());
            var weatherInfo = JsonSerializer.Deserialize<WeatherInfo>(
                jsonMessage.RootElement.GetProperty("weatherInfo").GetRawText());
            var locationInfo = JsonSerializer.Deserialize<LocationInfo>(
                jsonMessage.RootElement.GetProperty("locationInfo").GetRawText());
            var exchangeRateInfo = JsonSerializer.Deserialize<ExchangeRateInfo>(
                jsonMessage.RootElement.GetProperty("exchangeRateInfo").GetRawText());

            return (watchRequest, weatherInfo, locationInfo, exchangeRateInfo);
        }

        /// <summary>
        /// Save Data into persistent storage
        /// </summary>
        private async Task SaveData(
            WatchRequest watchRequest, WeatherInfo weatherInfo, LocationInfo locationInfo, ExchangeRateInfo exchangeRateInfo)
        {
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

            var requestData = _mapper.Map<RequestData>(watchRequest);
            requestData = _mapper.Map(weatherInfo, requestData);
            requestData = _mapper.Map(locationInfo, requestData);
            requestData = _mapper.Map(exchangeRateInfo, requestData);
            requestData.DeviceDataId = deviceData.Id;

            await dbConnection.GetTable<RequestData>().DataContext.InsertAsync(requestData);
        }
    }
}
