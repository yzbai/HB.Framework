﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HB.Framework.Common.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CacheEntityAttribute : Attribute
    {
        public string? CacheInstanceName { get; set; }


        public bool AllowMultipleRetrieve { get; set; }

        /// <summary>
        /// 在没有超多最多时间范围内，每次续命多久
        /// </summary>
        public TimeSpan? SlidingAliveTime { get; set; }

        /// <summary>
        /// 最多活多长时间
        /// </summary>
        public TimeSpan? MaxAliveTime { get; set; }

        public CacheEntityAttribute(bool allowMultipleRetrieve = false)
        {
            AllowMultipleRetrieve = allowMultipleRetrieve;
        }
    }
}