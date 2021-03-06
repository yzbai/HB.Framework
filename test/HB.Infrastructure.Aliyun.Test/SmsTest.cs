﻿using HB.FullStack.Common.Sms;
using HB.Infrastructure.Aliyun.Sms;
using System;
using Xunit;
using Xunit.Abstractions;

namespace HB.Infrastructure.Aliyun.Test
{
    public class SmsTest : IClassFixture<ServiceFixture>
    {
        private readonly ISmsService _smsBiz;
        private readonly ServiceFixture _fixture;
        private readonly ITestOutputHelper _output;

        public SmsTest(ITestOutputHelper output, ServiceFixture testFixture)
        {
            _output = output;
            _fixture = testFixture;
            _smsBiz = _fixture.ThrowIfNull(nameof(testFixture)).SmsService;
        }

        [Theory]
        [InlineData("15190208956")]
        [InlineData("18015323958")]
        public void SendSms(string mobile)
        {
            _smsBiz.SendValidationCode(mobile);
        }
    }
}
