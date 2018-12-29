﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using HB.Framework.EventBus.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace HB.Infrastructure.Redis.Test
{
    public class RedisEventBusTest : IClassFixture<ServiceFixture>, IEventHandler
    {
        private ITestOutputHelper _output;
        private IEventBus _eventBus;

        public RedisEventBusTest(ITestOutputHelper testOutputHelper, ServiceFixture serviceFixture)
        {
            _output = testOutputHelper;
            _eventBus = serviceFixture.EventBus;
        }

        public string EventType { get; set; }

        public void Handle(string jsonData)
        {
            _output.WriteLine(jsonData);
        }

        [Fact]
        public void TestEventBus()
        {
            //set handler
            EventType = "User.Upload.HeadImage";

            _eventBus.Subscribe(this);

            _eventBus.StartHandle(EventType);

            Thread.Sleep(2 * 1000);

            for(int i = 0; i < 100; ++i)
            {
                _eventBus.PublishAsync(new EventMessage(EventType, $"Hello, Just say {i} times."));
            }

            Thread.Sleep(1 * 60 * 1000);

            _eventBus.UnSubscribe(EventType);
        }
    }
}