using Newtonsoft.Json;

namespace IoTSensorReadingsFromPiToAzure
{
    public class BME280PlusLTR559 : BME280
    {
        public string Lux { get; set; }
        public string Proximity { get; set; }
        public BME280PlusLTR559()
                    : base() { }

        public BME280PlusLTR559(string deviceId, string temp, string pressure, string humidity, string altitude, string lux, string prox)
                    : base(deviceId, temp, pressure, humidity, altitude)
        {
            Lux = lux;
            Proximity = prox;
        }

        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
