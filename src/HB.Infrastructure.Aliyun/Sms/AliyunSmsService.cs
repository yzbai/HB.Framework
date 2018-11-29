﻿using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Aliyun.Acs.Core;
using HB.Framework.Common;
using System.Threading.Tasks;
using Aliyun.Acs.Core.Http;
using Aliyun.Acs.Dysmsapi.Model.V20170525;
using HB.Component.Resource.Sms.Entity;
using System.Globalization;
using Polly;
using Aliyun.Acs.Core.Exceptions;
using HB.Infrastructure.Aliyun.Sms.Transform;
using System;
using Microsoft.Extensions.Caching.Distributed;
using HB.Component.Resource.Sms;

namespace HB.Infrastructure.Aliyun.Sms
{
    public class AliyunSmsService : ISmsService
    {
        private AliyunSmsOptions _options;
        private IAcsClient _client;
        private ISmsCodeBiz _smsCodeBiz;
        private readonly ILogger _logger;
        private IDistributedCache _cache;

        public AliyunSmsService(IAcsClientManager acsClientManager, ISmsCodeBiz smsCodeBiz, IOptions<AliyunSmsOptions> options, ILogger<AliyunSmsService> logger) 
        {
            _options = options.Value;
            _client = acsClientManager.GetAcsClient(_options.ProductName);
            _logger = logger;
            _smsCodeBiz = smsCodeBiz;
        }

        public Task<SendResult> SendValidationCode(string mobile, out string smsCode)
        {
            smsCode = _smsCodeBiz.GenerateNewSmsCode(_options.TemplateIdentityValidation.CodeLength);
            
            SendSmsRequest request = new SendSmsRequest
            {
                AcceptFormat = FormatType.JSON,
                SignName = _options.SignName,
                TemplateCode = _options.TemplateIdentityValidation.TemplateCode,
                PhoneNumbers = mobile,
                TemplateParam = string.Format(GlobalSettings.Culture, "{{\"{0}\":\"{1}\", \"{2}\":\"{3}\"}}", 
                    _options.TemplateIdentityValidation.ParamCode, 
                    smsCode, 
                    _options.TemplateIdentityValidation.ParamProduct, 
                    _options.TemplateIdentityValidation.ParamProductValue)
            };

            string cachedSmsCode = smsCode;

            return PolicyManager.Default(_logger).ExecuteAsync(async ()=> {
                Task<SendSmsResponse> task = new Task<SendSmsResponse>(() => _client.GetAcsResponse(request));
                task.Start(TaskScheduler.Default);

                SendSmsResponse result = await task.ConfigureAwait(false);

                if (result.Code == "OK")
                {
                    _smsCodeBiz.CacheSmsCode(mobile, cachedSmsCode, _options.TemplateIdentityValidation.ExpireMinutes);
                }

                return SendResultTransformer.Transform(result);
            });
        }

        public bool Validate(string mobile, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            string cachedSmsCode = _smsCodeBiz.GetSmsCodeFromCache(mobile);

            return string.Equals(code, cachedSmsCode, GlobalSettings.Comparison);
        }

        
    }
}
