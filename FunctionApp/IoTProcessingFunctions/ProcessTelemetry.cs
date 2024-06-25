using System;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure;
using Azure.Data.Tables;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace IoTProcessingFunctions
{
    public class ProcessTelemetry
    {
        private readonly ILogger<ProcessTelemetry> _logger;

        public ProcessTelemetry(ILogger<ProcessTelemetry> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ProcessTelemetry))]
        public void Run([EventHubTrigger("pisensordatahub", Connection = "IoTHubConnection")] EventData[] events)
        {
            foreach (EventData @event in events)
            {
                var bodyInfo = @event.Body.Span;
                var message = Encoding.UTF8.GetString(bodyInfo);
                _logger.LogInformation("Event Body: {message}", message);

                var telemetry = JsonConvert.DeserializeObject<Telemetry>(message);

                telemetry.Timestamp = DateTime.UtcNow;
                telemetry.PartitionKey = telemetry.DeviceId;
                telemetry.RowKey = DateTime.UtcNow.Ticks.ToString();
                char degreeSymbol = '\u00B0';
                telemetry.TemperatureFarenheight = GetFDegrees(telemetry.TemperatureCelsius);
                telemetry.TemperatureCelsius = telemetry.TemperatureCelsius.Replace('?', degreeSymbol);
                
                _logger.LogInformation("Telemetry: {telemetry}", telemetry);

                var storageCNSTR = Environment.GetEnvironmentVariable("EventTelemetryStorage");
                var tableServiceClient = new TableServiceClient(storageCNSTR);
                var tableClient = tableServiceClient.GetTableClient(
                    tableName: "SensorReadings"
                );

                try
                {
                    tableClient.AddEntity(telemetry);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                    _logger.LogError(ex.ToString());
                }

                var cosmosCNSTR = Environment.GetEnvironmentVariable("CosmosDBConnection");
                
                var cosmosDBName = Environment.GetEnvironmentVariable("CosmosDBName");
                var cosmosContainerId = Environment.GetEnvironmentVariable("CosmosContainerId");

                _logger.LogInformation(cosmosCNSTR);
                _logger.LogInformation(cosmosDBName);
                _logger.LogInformation(cosmosContainerId);

                var telemetryCosmos = JsonConvert.DeserializeObject<TelemetryData>(message);
                telemetryCosmos.Id = Guid.NewGuid().ToString();
                telemetryCosmos.TemperatureFarenheight = GetFDegrees(telemetryCosmos.TemperatureCelsius);
                telemetryCosmos.TemperatureCelsius = telemetryCosmos.TemperatureCelsius.Replace('?', degreeSymbol);
                _logger.LogInformation($"{telemetryCosmos}");
                try
                {
                    using (CosmosClient client = new CosmosClient(cosmosCNSTR))
                    {
                        //Get database
                        var db = client.GetDatabase(cosmosDBName);

                        //Get Container:
                        var container = db.GetContainer(cosmosContainerId);

                        //Add Item
                        container.CreateItemAsync(telemetryCosmos).Wait();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                    _logger.LogError(ex.ToString());
                }
                
            }
        }

        private double? GetFDegrees(string tempC)
        {
            var cdeg = tempC.Replace("?C", string.Empty);
            bool success = double.TryParse(cdeg, out double result);
            
            if (success) {
                var degF = (result * 9 / 5) + 32;
                return degF; 
            }
            return null;
        }
    }
}
