using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessingFunctions
{
    public class TelemetryData
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string Lux { get; set; }
        public string Proximity { get; set; }
        public string TemperatureCelsius { get; set; }
        public double? TemperatureFarenheight { get; set; }
        public string PressureHectoPascals { get; set; }
        public string RelativeHumidityPercent { get; set; }
        public string EstimatedAltitudeMeters { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
