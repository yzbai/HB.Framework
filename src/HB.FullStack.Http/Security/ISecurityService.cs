﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HB.FullStack.Common.Api;
using Microsoft.AspNetCore.Http;

namespace HB.FullStack.Server.Security
{
    public interface ISecurityService
    {
        Task<bool> NeedPublicResourceTokenAsync(ApiRequest? apiRequest);
        Task<byte[]> ProcessFormFileAsync(IFormFile? formFile, string[] permittedExtensions, long sizeLimit);
    }
}