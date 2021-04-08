﻿using System;
using System.Collections.Generic;
using System.Reflection;

using HB.FullStack.Common;

namespace HB.FullStack.Cache
{
    public static class CacheEntityDefFactory
    {
        private static readonly Dictionary<Type, CacheEntityDef> _defDict = new Dictionary<Type, CacheEntityDef>();

        private static readonly object _lockObj = new object();

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CacheException"></exception>
        public static CacheEntityDef Get<TEntity>() where TEntity : Entity, new()
        {
            Type entityType = typeof(TEntity);

            if (!_defDict.ContainsKey(entityType))
            {
                lock (_lockObj)
                {
                    if (!_defDict.ContainsKey(entityType))
                    {
                        _defDict[entityType] = CreateEntityDef(entityType);
                    }
                }
            }

            return _defDict[entityType];

        }

        /// <summary>
        /// CreateEntityDef
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="CacheException"></exception>
        private static CacheEntityDef CreateEntityDef(Type entityType)
        {
            CacheEntityDef def = new()
            {
                Name = entityType.Name
            };

            CacheEntityAttribute? cacheEntityAttribute = entityType.GetCustomAttribute<CacheEntityAttribute>();

            if (cacheEntityAttribute == null)
            {
                def.IsCacheable = false;

                return def;
            }

            def.IsCacheable = true;

            def.CacheInstanceName = cacheEntityAttribute.CacheInstanceName;

            def.SlidingTime = cacheEntityAttribute.SlidingSeconds == -1 ? null : (TimeSpan?)TimeSpan.FromSeconds(cacheEntityAttribute.SlidingSeconds);

            def.AbsoluteTimeRelativeToNow = cacheEntityAttribute.MaxAliveSeconds == -1 ? null : (TimeSpan?)TimeSpan.FromSeconds(cacheEntityAttribute.MaxAliveSeconds);

            if (def.SlidingTime > def.AbsoluteTimeRelativeToNow)
            {
                throw Exceptions.CacheSlidingTimeBiggerThanMaxAlive(type: def.Name);
            }

            bool foundkeyAttribute = false;

            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (!foundkeyAttribute)
                {
                    CacheKeyAttribute? keyAttribute = propertyInfo.GetCustomAttribute<CacheKeyAttribute>();

                    if (keyAttribute != null)
                    {
                        def.KeyProperty = propertyInfo;
                        foundkeyAttribute = true;

                        continue;
                    }
                }

                CacheDimensionKeyAttribute? dimensionKeyAttribute = propertyInfo.GetCustomAttribute<CacheDimensionKeyAttribute>();

                if (dimensionKeyAttribute != null)
                {
                    def.Dimensions.Add(propertyInfo);
                }
            }

            if (def.KeyProperty == null)
            {
                throw Exceptions.CacheEntityNotHaveKeyAttribute(type: entityType.FullName);
            }

            return def;
        }
    }
}
