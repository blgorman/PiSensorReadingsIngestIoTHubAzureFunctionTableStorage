using Newtonsoft.Json;

namespace IoTSensorReadingsFromPiToAzure
{
    public class BME280
    {
        public string DeviceId {get; set;}
        public string TemperatureCelsius { get; set; }
        public string PressureHectoPascals { get; set; }
        public string RelativeHumidityPercent { get; set; }
        public string EstimatedAltitudeMeters { get; set; }

        public BME280() { }

        public BME280(string deviceId, string temp, string pressure, string humidity, string altitude)
        {
            DeviceId = deviceId;
            TemperatureCelsius = temp;
            PressureHectoPascals = pressure;
            RelativeHumidityPercent = humidity;
            EstimatedAltitudeMeters = altitude;
        }

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
