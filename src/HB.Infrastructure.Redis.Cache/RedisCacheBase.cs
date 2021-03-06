﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using HB.FullStack.Cache;
using HB.Infrastructure.Redis.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace HB.Infrastructure.Redis.Cache
{
    internal class RedisCacheBase
    {
        protected const int _invalidationVersionExpirySeconds = 60;

        private readonly RedisCacheOptions _options;
        protected readonly ILogger _logger;

        private readonly IDictionary<string, RedisInstanceSetting> _instanceSettingDict;
        private readonly IDictionary<string, LoadedLuas> _loadedLuaDict = new Dictionary<string, LoadedLuas>();

        public RedisCacheBase(IOptions<RedisCacheOptions> options, ILogger logger)
        {
            _options = options.Value;
            _logger = logger;
            _instanceSettingDict = _options.ConnectionSettings.ToDictionary(s => s.InstanceName);

            InitLoadedLuas();
        }

        public string DefaultInstanceName => _options.DefaultInstanceName ?? _options.ConnectionSettings[0].InstanceName;

        public void Close()
        {
            foreach (var kv in _instanceSettingDict)
            {
                RedisInstanceManager.Close(kv.Value);
            }
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 各服务器反复Load也没有关系
        /// </summary>
        protected void InitLoadedLuas()
        {
            foreach (RedisInstanceSetting setting in _options.ConnectionSettings)
            {
                IServer server = RedisInstanceManager.GetServer(setting, _logger);
                LoadedLuas loadedLuas = new LoadedLuas
                {
                    LoadedSetWithTimestampLua = server.ScriptLoad(RedisCache.LUA_SET_WITH_TIMESTAMP),
                    LoadedRemoveWithTimestampLua = server.ScriptLoad(RedisCache.LUA_REMOVE_WITH_TIMESTAMP),
                    LoadedGetAndRefreshLua = server.ScriptLoad(RedisCache.LUA_GET_AND_REFRESH),

                    LoadedEntitiesGetAndRefreshLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_GET_AND_REFRESH),
                    LoadedEntitiesGetAndRefreshByDimensionLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_GET_AND_REFRESH_BY_DIMENSION),
                    LoadedEntitiesSetLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_SET),
                    LoadedEntitiesRemoveLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_REMOVE),
                    LoadedEntitiesRemoveByDimensionLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_REMOVE_BY_DIMENSION),

                    LoadedEntitiesForcedRemoveLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_REMOVE_FORECED_NO_VERSION),
                    LoadedEntitiesForcedRemoveByDimensionLua = server.ScriptLoad(RedisCache.LUA_ENTITIES_REMOVE_BY_DIMENSION_FORCED_NO_VERSION),
                };

                _loadedLuaDict[setting.InstanceName] = loadedLuas;
            }
        }

        /// <exception cref="CacheException"></exception>
        protected LoadedLuas GetDefaultLoadLuas()
        {
            return GetLoadedLuas(DefaultInstanceName);
        }

        /// <exception cref="CacheException"></exception>
        protected LoadedLuas GetLoadedLuas(string? instanceName)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = DefaultInstanceName;
            }

            if (_loadedLuaDict.TryGetValue(instanceName, out LoadedLuas? loadedLuas))
            {
                return loadedLuas;
            }

            InitLoadedLuas();

            if (_loadedLuaDict.TryGetValue(instanceName, out LoadedLuas? loadedLuas2))
            {
                return loadedLuas2;
            }

            throw Exceptions.CacheLoadedLuaNotFound(instanceName: instanceName);
        }

        /// <exception cref="CacheException"></exception>
        protected async Task<IDatabase> GetDatabaseAsync(string? instanceName)
        {
            instanceName ??= DefaultInstanceName;

            if (_instanceSettingDict.TryGetValue(instanceName, out RedisInstanceSetting? setting))
            {
                return await RedisInstanceManager.GetDatabaseAsync(setting, _logger).ConfigureAwait(false);
            }

            throw Exceptions.InstanceNotFound(instanceName: instanceName);
        }

        /// <exception cref="CacheException"></exception>
        protected Task<IDatabase> GetDefaultDatabaseAsync()
        {
            return GetDatabaseAsync(DefaultInstanceName);
        }

        /// <exception cref="CacheException"></exception>
        protected IDatabase GetDatabase(string? instanceName)
        {
            instanceName ??= DefaultInstanceName;

            if (_instanceSettingDict.TryGetValue(instanceName, out RedisInstanceSetting? setting))
            {
                return RedisInstanceManager.GetDatabase(setting, _logger);
            }

            throw Exceptions.InstanceNotFound(instanceName);
        }

        /// <exception cref="CacheException"></exception>
        protected IDatabase GetDefaultDatabase()
        {
            return GetDatabase(DefaultInstanceName);
        }

        protected string GetRealKey(string entityName, string key)
        {
            return _options.ApplicationName + entityName + key;
        }

        protected string GetEntityDimensionKey(string entityName, string dimensionKeyName, string dimensionKeyValue)
        {
            return GetRealKey(entityName, dimensionKeyName + dimensionKeyValue);
        }

        /// <exception cref="CacheException"></exception>
        protected static void ThrowIfNotADimensionKeyName(string dimensionKeyName, CacheEntityDef entityDef)
        {
            if (!entityDef.Dimensions.Any(p => p.Name == dimensionKeyName))
            {
                throw Exceptions.NoSuchDimensionKey(type:entityDef.Name, dimensionKeyName:dimensionKeyName);
            }
        }

        /// <summary>
        /// ThrowIfNotCacheEnabled
        /// </summary>
        /// <param name="entityDef"></param>
        /// <exception cref="CacheException"></exception>
        protected static void ThrowIfNotCacheEnabled(CacheEntityDef entityDef)
        {
            if (!entityDef.IsCacheable)
            {
                throw Exceptions.NotEnabledForEntity(type:entityDef.Name);
            }
        }

        protected static long? GetInitialExpireSeconds(long? absoluteExpireUnixSeconds, long? slideSeconds)
        {
            //参见Readme.txt
            if (slideSeconds != null)
            {
                return slideSeconds.Value;
            }

            if (absoluteExpireUnixSeconds != null)
            {
                return absoluteExpireUnixSeconds - TimeUtil.UtcNowUnixTimeSeconds;
            }

            return null;
        }
    }
}