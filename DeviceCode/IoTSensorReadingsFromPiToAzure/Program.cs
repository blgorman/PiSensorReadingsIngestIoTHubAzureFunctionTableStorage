using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using System.Device.I2c;
using System.Text;
using System.Linq;

namespace IoTSensorReadingsFromPiToAzure
{
    public class Program
    {
        private static IConfiguration _configuration;
        private static DeviceClient _deviceClient;
        private static bool _shouldShowTelemetryOutput = true;
        private static string _deviceConnectionString = string.Empty;
        private static string _deviceId = string.Empty;
        private static int _telemetryReadInterval = 30;
        private static int _telemetryReadDurationSeconds = 90;

        public static async Task Main(string[] args)
        {
            BuildOptions();
            BuildConfigValues();
            await ReadSensorData();

            Console.WriteLine("Program Completed");
        }

        private static void BuildOptions()
        {
            _configuration = ConfigurationBuilderSingleton.ConfigurationRoot;
        }

        private static string GetConfigValue(string variableKey)
        {
            var variableValue = Environment.GetEnvironmentVariable(variableKey);
            if (string.IsNullOrWhiteSpace(variableValue))
            {
                variableValue = _configuration[variableKey];
            }

            return variableValue;
        }

        private static void BuildConfigValues()
        {
            _shouldShowTelemetryOutput = Convert.ToBoolean(GetConfigValue("Device:OutputTelemetry"));
            Console.WriteLine($"Show Telemetry: {_shouldShowTelemetryOutput}");
            //get device connection string
            _deviceConnectionString = GetConfigValue("Device:AzureConnectionString");
            _deviceId = GetConfigValue("Device:DeviceId");
            var keyIndex = _deviceConnectionString.IndexOf("SharedAccessKey");
            var safeShowConStr = _deviceConnectionString.Substring(0, keyIndex);
            safeShowConStr += "SharedAccessKey=*****************";
            Console.WriteLine($"Connection string: {safeShowConStr}");

            //get configured read duration [default/min => 15 seconds]
            var duration = GetConfigValue("Device:TelemetryReadDurationInSeconds");
            Console.WriteLine($"Telemetry Read Duration set to: {duration} seconds");

            //update duration from value if > 15
            if (!string.IsNullOrWhiteSpace(duration))
            {
                int.TryParse(duration, out int readDurationSeconds);
                if (readDurationSeconds > 15)
                {
                    _telemetryReadDurationSeconds = readDurationSeconds;
                }
            }
        }


        private static async Task ReadSensorData()
        {
            //set up the device client
            _deviceClient = DeviceClient.CreateFromConnectionString(
                    _deviceConnectionString,
                    TransportType.Mqtt);

            int duration = 90;
            bool success = false;
            while (!success)
            {
                Console.WriteLine("How long should I run in seconds?");
                success = int.TryParse(Console.ReadLine(), out duration);
                if (!success)
                {
                    Console.WriteLine("Invalid entry, please try again");
                }
            }
            _telemetryReadDurationSeconds = duration > 30 ? duration : 30;

            //set time to end reading data
            var endReadingsAtTime = DateTime.Now.AddSeconds(_telemetryReadDurationSeconds);

            //utilize the library to read Bme280 data
            var i2cSettings = new I2cConnectionSettings(1, Bme280.SecondaryI2cAddress);
            using I2cDevice i2cDevice = I2cDevice.Create(i2cSettings);
            using var bme280 = new Bme280(i2cDevice);

            //device readings created by python script execution on the device:
            int measurementTime = bme280.GetMeasurementDuration();
            var command = "python";
            var script = @"./singlelight.py";
            var args = $"{script}"; 

            //loop until duration
            while(DateTime.Now < endReadingsAtTime)
            {
                bme280.SetPowerMode(Bmx280PowerMode.Forced);
                Thread.Sleep(measurementTime);

                //read values for temp/pressure/humidity/altitude
                bme280.TryReadTemperature(out var tempValue);
                bme280.TryReadPressure(out var preValue);
                bme280.TryReadHumidity(out var humValue);
                bme280.TryReadAltitude(out var altValue);

                //set base values:
                var envData = new EnviroSensorData();
                envData.Temperature = $"{tempValue.DegreesCelsius:0.#}\u00B0C";
                envData.Humidity = $"{humValue.Percent:#.##}%";
                envData.Pressure = $"{preValue.Hectopascals:#.##} hPa";
                envData.Altitude = $"{altValue.Meters:#} m";

                //read light and proximity values
                string lightProx = string.Empty;
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    StreamReader sr = process.StandardOutput;
                    lightProx = sr.ReadToEnd();
                    process.WaitForExit();
                }

                var result = lightProx.Split('\'');
                envData.Light = result[3];
                envData.Proximity = result[7];

                if(_shouldShowTelemetryOutput)
                {
                    Console.WriteLine(new string('*', 80));
                    Console.WriteLine("* Telemetry Data: ");
                    Console.WriteLine(envData);
                    Console.WriteLine(new string('*', 80));
                }

                var telemetryObject = new BME280PlusLTR559(_deviceId, envData.Temperature, envData.Pressure, 
                                                            envData.Humidity, envData.Altitude, 
                                                            envData.Light, envData.Proximity);

                //telemetry object has the full output:
                var telemetryMessage = telemetryObject.ToJson();
                //create the message to send to the hub
                var msg = new Message(Encoding.ASCII.GetBytes(telemetryMessage));
                //send the telemetry to azure
                await _deviceClient.SendEventAsync(msg);

                //output result
                Console.WriteLine($"Telemetry sent {DateTime.Now}");
                Thread.Sleep(_telemetryReadInterval * 1000);
            }

            Console.WriteLine("All telemetry read");
        }
    }
}
