﻿#nullable enable

using System;

namespace HB.FullStack.Database.Entities
{
    /// <summary>
    /// 标识字段为主键
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AutoIncrementPrimaryKeyAttribute : PrimaryKeyAttribute
    {
        public AutoIncrementPrimaryKeyAttribute()
        {
        }
    }
}