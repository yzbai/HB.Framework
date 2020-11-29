﻿using HB.FullStack.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace HB.FullStack.Client.Api
{
    public class EndpointSettings : ValidatableObject
    {
        /// <summary>
        /// 产品名，一般为站点类名
        /// </summary>
        [Required]
        public string? ProductName { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [Required]
        public string? Version { get; set; }

        /// <summary>
        /// url地址
        /// </summary>
        [Required]
        public Uri? Url { get; set; }

        public bool NeedHttpMethodOveride { get; set; } = true;

        public JwtSettings JwtSettings { get; set; } = new JwtSettings();


        public static string GetHttpClientName(EndpointSettings endpoint)
        {
            return endpoint.ProductName + "_" + endpoint.Version;
        }
    }
}