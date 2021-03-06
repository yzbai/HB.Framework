﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HB.Infrastructure.Aliyun.Oss;
using Xunit;
using Xunit.Abstractions;

namespace HB.Infrastructure.Aliyun.Test
{
    public class OssTest : IClassFixture<ServiceFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly IAliyunOssService _oss;

        public OssTest(ITestOutputHelper output, ServiceFixture serviceFixture)
        {
            _output = output;
            _oss = serviceFixture.ThrowIfNull(nameof(serviceFixture)).AliyunOssService;
        }

        //[Theory]
        //[InlineData("ahabit", "12345678-1234-1234-1234-123456789ABC")]
        //public async Task UserReadSts_TestAsync(string bucket, string userGuid)
        //{
        //    StsRoleCredential credential = await _oss.GetUserRoleCredentialAsync(bucket, userGuid, true);

        //    Assert.NotNull(credential.SecurityToken);
        //}
    }
}