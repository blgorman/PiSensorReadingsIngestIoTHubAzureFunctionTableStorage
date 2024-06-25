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

                var storageCNSTR = Environment.GetEnvironmentVariable("EventTelemetryStorage");
                var tableServiceClient = new TableServiceClient(storageCNSTR);
                var tableClient = tableServiceClient.GetTableClient(
                    tableName: "SensorReadings"
                );
                

                telemetry.Timestamp = DateTime.UtcNow;
                telemetry.PartitionKey = telemetry.DeviceId;
                telemetry.RowKey = DateTime.UtcNow.Ticks.ToString();
                _logger.LogInformation("Telemetry: {telemetry}", telemetry);

                try
                {
                    tableClient.AddEntity(telemetry);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                    _logger.LogError(ex.ToString());
                }
                
            }
        }

        
    }
}
