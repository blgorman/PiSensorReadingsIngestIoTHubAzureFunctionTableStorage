using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessingFunctions
{
    public class Telemetry : ITableEntity
    {
        public string DeviceId { get; set; }
        public string Lux { get; set; }
        public string Proximity { get; set; }
        public string TemperatureCelsius { get; set; }
        public string PressureHectoPascals { get; set; }
        public string RelativeHumidityPercent { get; set; }
        public string EstimatedAltitudeMeters { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public override string ToString()
        {
            return GetType().GetProperties()
            .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "(null)"))
            .Aggregate(
                new StringBuilder(),
                (sb, pair) => sb.AppendLine($"{pair.Name}: {pair.Value}"),
                sb => sb.ToString());
        }
    }
}
