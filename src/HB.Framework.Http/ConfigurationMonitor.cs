using System;
using System.Linq;
using HB.Framework.Http.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HB.Framework.Http
{
    public class ConfigurationMonitor : IConfigurationMonitor
    {
        private byte[] _appsettingsHash = new byte[20];
        private byte[] _appsettingsEnvHash = new byte[20];
        private readonly IWebHostEnvironment _env;

        private readonly IHostApplicationLifetime _applicationLifeTime;
        private readonly ILogger _logger;

 
        public ConfigurationMonitor(IConfiguration config, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, ILogger<ConfigurationMonitor> logger)
        {
            _env = env;
            _applicationLifeTime = applicationLifetime;
            _logger = logger;

            ChangeToken.OnChange<IConfigurationMonitor>(
                () => config.GetReloadToken(),
                InvokeChanged,
                this);
        }

        public bool MonitoringEnabled { get; set; } = false;
 
 
        private void InvokeChanged(IConfigurationMonitor state)
        {
            if (MonitoringEnabled)
            {
                byte[] appsettingsHash = FileUtil.ComputeFileHash("appSettings.json");
                byte[] appsettingsEnvHash = FileUtil.ComputeFileHash($"appSettings.{_env.EnvironmentName}.json");

                if (!_appsettingsHash.SequenceEqual(appsettingsHash) || 
                    !_appsettingsEnvHash.SequenceEqual(appsettingsEnvHash))
                {
                    string message = $"State updated at {DateTime.Now}";
                  

                    _appsettingsHash = appsettingsHash;
                    _appsettingsEnvHash = appsettingsEnvHash;

                    _logger.LogInformation(message);
                    _logger.LogInformation(Resources.ConfigurationFileChangedMessage);
                    _logger.LogInformation(Resources.SystemWillRestartMessage);


                    //TODO: ����Źر�Ӧ��ǰ����Ҫ���Ĺ���

                    _applicationLifeTime.StopApplication();
                }
            }
        }

        public void EnableMonitoring()
        {
            MonitoringEnabled = true;
        }

        public void DisableMonitoring()
        {
            MonitoringEnabled = false;
        }
    }
}