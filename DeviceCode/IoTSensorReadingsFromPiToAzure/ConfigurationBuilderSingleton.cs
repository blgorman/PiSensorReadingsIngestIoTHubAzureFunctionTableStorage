
using Microsoft.Extensions.Configuration;

namespace IoTSensorReadingsFromPiToAzure
{
    public sealed class ConfigurationBuilderSingleton
    {
        private static ConfigurationBuilderSingleton _instance = null;
        private static readonly object instanceLock = new object();

        private static IConfigurationRoot _configuration;
        private static IConfigurationBuilder _builder;

        private ConfigurationBuilderSingleton()
        {
            _builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddEnvironmentVariables()
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            
            _configuration = _builder.Build();
        }

        public static ConfigurationBuilderSingleton Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ConfigurationBuilderSingleton();
                    }
                    return _instance;
                }
            }
        }

        public static IConfigurationRoot ConfigurationRoot
        {
            get
            {
                if (_configuration == null) { var x = Instance; }
                return _configuration;
            }
        }

    }
}