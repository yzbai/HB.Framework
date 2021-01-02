﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HB.FullStack.Common.Api;
using HB.FullStack.Common.Resources;

namespace System.Net.Http
{
    [Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "<Pending>")]
    public class EmptyResponse
    {
        public static EmptyResponse Value { get; }

        static EmptyResponse()
        {
            Value = new EmptyResponse();
        }

        private EmptyResponse() { }
    }

    public static class HttpClientApiExtensions
    {
        private static readonly Type _emptyResponse = typeof(EmptyResponse);

        public static async Task<TResponse?> SendAsync<TResource, TResponse>(this HttpClient httpClient, ApiRequest<TResource> request) where TResource : Resource where TResponse : class
        {
            using HttpResponseMessage responseMessage = await httpClient.SendCoreAsync(request).ConfigureAwait(false);

            await ThrowIfNotSuccessedAsync(responseMessage).ConfigureAwait(false);

            if (typeof(TResponse) == _emptyResponse)
            {
                return (TResponse)(object)EmptyResponse.Value;
            }
            else
            {
                TResponse? response = await responseMessage.DeSerializeJsonAsync<TResponse>().ConfigureAwait(false);

                //if (response == null)
                //{
                //    throw new ApiException(ErrorCode.ApiNullReturn, responseMessage.StatusCode);
                //}

                return response;
            }
        }

        private static async Task<HttpResponseMessage> SendCoreAsync<T>(this HttpClient httpClient, ApiRequest<T> request) where T : Resource
        {
            try
            {
                using HttpRequestMessage requestMessage = request.ToHttpRequestMessage();

                return await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(ErrorCode.ApiUnkown, System.Net.HttpStatusCode.BadRequest, $"ApiRequestUtils.GetResponse {request.GetResourceName()}", ex);
            }
        }

        private static HttpRequestMessage ToHttpRequestMessage<T>(this ApiRequest<T> request) where T : Resource
        {
            HttpMethod httpMethod = request.GetHttpMethod();

            if (request.GetNeedHttpMethodOveride() && (httpMethod == HttpMethod.Put || httpMethod == HttpMethod.Delete))
            {
                request.SetHeader("X-HTTP-Method-Override", httpMethod.Method);
                httpMethod = HttpMethod.Post;
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(httpMethod, BuildUrl(request));

            //Get的参数也放到body中去

            if (request is FileUpdateRequest<T> fileRequest)
            {
                httpRequest.Content = BuildMultipartContent(fileRequest);
            }
            else
            {
                //TODO: .net 5以后，使用JsonContent
                httpRequest.Content = new StringContent(SerializeUtil.ToJson(request), Encoding.UTF8, "application/json");
            }

            foreach (var kv in request.GetHeaders())
            {
                httpRequest.Headers.Add(kv.Key, kv.Value);
            }

            return httpRequest;
        }

        [Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static MultipartFormDataContent BuildMultipartContent<T>(FileUpdateRequest<T> fileRequest) where T : Resource
        {
            MultipartFormDataContent content = new MultipartFormDataContent();

            string byteArrayContentName = fileRequest.GetBytesPropertyName();
            IEnumerable<byte[]> bytess = fileRequest.GetBytess();
            IEnumerable<string> fileNames = fileRequest.GetFileNames();

            int num = 0;

            foreach (string fileName in fileNames)
            {
                byte[] bytes = bytess.ElementAt(num++);

                ByteArrayContent byteArrayContent = new ByteArrayContent(bytes);
                content.Add(byteArrayContent, byteArrayContentName, fileName);
            }

            //request.GetParameters().ForEach(kv =>
            //{
            //    content.Add(new StringContent(kv.Value), kv.Key);
            //});

            content.Add(new StringContent(SerializeUtil.ToJson(fileRequest), Encoding.UTF8, "application/json"));

            return content;
        }

        private static string BuildUrl(ApiRequest request)
        {
            StringBuilder requestUrlBuilder = new StringBuilder();

            if (!request.GetApiVersion().IsNullOrEmpty())
            {
                requestUrlBuilder.Append(request.GetApiVersion());
            }

            if (!request.GetResourceName().IsNullOrEmpty())
            {
                requestUrlBuilder.Append('/');
                requestUrlBuilder.Append(request.GetResourceName());
            }

            if (!request.GetCondition().IsNullOrEmpty())
            {
                requestUrlBuilder.Append('/');
                requestUrlBuilder.Append(request.GetCondition());
            }

            //添加噪音
            IDictionary<string, string?> parameters = new Dictionary<string, string?>
            {
                { ClientNames.RandomStr, ApiRequest.GetRandomStr() },
                { ClientNames.Timestamp, TimeUtil.UtcNowUnixTimeMilliseconds.ToString(CultureInfo.InvariantCulture) },
                { ClientNames.DeviceId, request.DeviceId }//额外添加DeviceId，为了验证jwt中的DeviceId与本次请求deviceiId一致
            };

            string? query = parameters.ToHttpValueCollection().ToString();
            requestUrlBuilder.Append('?');
            requestUrlBuilder.Append(query);

            //放到Body中去
            //if (request.GetHttpMethod() == HttpMethod.Get)
            //{
            //    string query = request.GetParameters().ToHttpValueCollection().ToString();

            //    if (!query.IsNullOrEmpty())
            //    {
            //        requestUrlBuilder.Append('?');
            //        requestUrlBuilder.Append(query);
            //    }
            //}

            return requestUrlBuilder.ToString();
        }

        public static async Task ThrowIfNotSuccessedAsync(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                return;
            }

            ApiError? apiError = await responseMessage.DeSerializeJsonAsync<ApiError>().ConfigureAwait(false);

            if (apiError == null)
            {
                throw new ApiException(ErrorCode.ApiUnkown, responseMessage.StatusCode, responseMessage.ReasonPhrase);
            }
            else
            {
                throw new ApiException(apiError.Code, responseMessage.StatusCode, apiError.Message, apiError.ModelStates);
            }
        }
    }
}