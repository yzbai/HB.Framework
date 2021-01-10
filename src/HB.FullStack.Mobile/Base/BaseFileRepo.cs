﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HB.FullStack.Mobile.Api;
using HB.FullStack.Common.Api;

namespace HB.FullStack.Mobile.Repos
{
    public abstract class BaseFileRepo<TRes> : BaseRepo where TRes : ApiResource
    {
        protected IApiClient ApiClient { get; }

        protected BaseFileRepo(IApiClient apiClient)
        {
            ApiClient = apiClient;
        }

        /// <summary>
        /// UploadAsync
        /// </summary>
        /// <param name="fileSuffix"></param>
        /// <param name="fileDatas"></param>
        /// <param name="resources"></param>
        /// <returns></returns>
        /// <exception cref="System.ApiException"></exception>
        public Task UploadAsync(string fileSuffix, IEnumerable<byte[]> fileDatas, IEnumerable<TRes> resources)
        {
            InsureInternet();

            string suffix = fileSuffix.StartsWith('.') ? fileSuffix : "." + fileSuffix;

            var fileNames = resources.Select(r => $"{r.Id}{fileSuffix}");

            FileUpdateRequest<TRes> request = new FileUpdateRequest<TRes>(fileDatas, fileNames, resources);

            return ApiClient.UpdateAsync(request);
        }
    }
}