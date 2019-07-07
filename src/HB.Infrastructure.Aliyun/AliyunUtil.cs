﻿using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace HB.Infrastructure.Aliyun
{
    internal class AliyunUtil
    {
        public static void AddEndpoint(string productName, string regionId, string endpoint)
        {
            if (productName.IsNullOrEmpty() || regionId.IsNullOrEmpty() || endpoint.IsNullOrEmpty())
            {
                return;
            }

            DefaultProfile.AddEndpoint(productName + regionId, regionId, productName, endpoint);
        }

        public static IAcsClient CreateAcsClient(string regionId, string accessKeyId, string accessKeySecret)
        {

            DefaultProfile profile = DefaultProfile.GetProfile(regionId, accessKeyId, accessKeySecret);

            return new DefaultAcsClient(profile);
        }
    }
}