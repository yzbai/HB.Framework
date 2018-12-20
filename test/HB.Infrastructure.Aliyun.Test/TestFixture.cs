﻿using HB.Component.Resource.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace HB.Infrastructure.Aliyun.Test
{
    public class TestFixture
    {
        public static IConfiguration Configuration { get; private set; }

        public static IServiceProvider Services { get; private set; }

        public TestFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false);


            Configuration = configurationBuilder.Build();

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddOptions();

            serviceCollection.AddLogging(builder => {
                builder.AddConsole();
            });

            serviceCollection.AddDistributedMemoryCache();

            serviceCollection.AddAliyunService(Configuration.GetSection("Aliyun"));
            serviceCollection.AddAliyunSms(Configuration.GetSection("AliyunSms"));

            Services = serviceCollection.BuildServiceProvider();
        }

        public ISmsService SmsService => Services.GetRequiredService<ISmsService>();
    }
}
