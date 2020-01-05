﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HB.Infrastructure.Tencent
{
    internal class TCapthaClient : ITCapthaClient
    {
        private readonly ILogger _logger;
        private readonly TCapthaOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDictionary<string, ApiKeySetting> _apiKeySettings;

        public TCapthaClient(ILogger<TCapthaClient> logger, IOptions<TCapthaOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _options = options.Value;
            _httpClientFactory = httpClientFactory;

            _apiKeySettings = _options.ApiKeySettings.ToDictionary(s => s.AppId);
        }

        public async Task<bool> VerifyTicket(string appid, string ticket, string randstr, string userIp)
        {
            ThrowIf.NullOrEmpty(appid, nameof(appid));

            ThrowIf.NullOrEmpty(ticket, nameof(ticket));

            ThrowIf.NullOrEmpty(randstr, nameof(randstr));

            ThrowIf.NullOrEmpty(userIp, nameof(userIp));

            if (!_apiKeySettings.TryGetValue(appid, out ApiKeySetting apiKeySetting))
            {
                throw new ServiceException($"lack ApiKeySettings for AppId:{appid}");
            }

            string query = new Dictionary<string, string> {
                { "aid", apiKeySetting.AppId},
                { "AppSecretKey", apiKeySetting.AppSecretKey},
                {"Ticket",  ticket},
                {"Randstr", randstr},
                {"UserIP", userIp }
            }.ToHttpValueCollection().ToString();

            string requestUrl = _options.Endpoint.RemoveSuffix("/") + "?" + query;

            using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            HttpClient httpClient = _httpClientFactory.CreateClient(TCapthaOptions.EndpointName);

            string content;

            using (HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage).ConfigureAwait(false))
            {
                if (!responseMessage.IsSuccessStatusCode)
                {
                    return false;
                }

                content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                //TODO: 记录分析evil_level
            }
            try
            {
                int result = Convert.ToInt32(SerializeUtil.FromJson(content, "response"), GlobalSettings.Culture);

                if (result == 1)
                {
                    return true;
                }

                return false;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception jsonException)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogException(jsonException, $"TCaptha Response Parse Error. Content:{content}");

                return false;
            }
        }


    }
}

