﻿using System;
using System.Collections.Generic;
using System.Text;
using HB.Framework.KVStore;
using HB.Infrastructure.Redis.Direct;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HB.Infrastructure.Redis.Test
{
    public class ServiceFixture
    {
        public IConfiguration Configuration { get; private set; }

        public IServiceProvider Services { get; private set; }

        public ServiceFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);


            Configuration = configurationBuilder.Build();

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddOptions();

            serviceCollection.AddLogging(builder => {
                builder.AddConsole();
            });


            serviceCollection.AddRedis(Configuration.GetSection("RedisEngine"));

            Services = serviceCollection.BuildServiceProvider();
        }

        public IRedisDatabase RedisEngine => this.Services.GetRequiredService<IRedisDatabase>();

        public IKVStore KVStore => this.Services.GetRequiredService<IKVStore>();
    }
}
