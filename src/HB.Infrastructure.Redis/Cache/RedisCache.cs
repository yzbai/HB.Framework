﻿using HB.Framework.Common.Cache;
using HB.Framework.Common.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HB.Infrastructure.Redis.Cache
{
    internal partial class RedisCache : ICache
    {
        private readonly ILogger _logger;
        private readonly RedisCacheOptions _options;
        private readonly IDictionary<string, RedisInstanceSetting> _instanceSettingDict;
        private readonly IDictionary<string, LoadedLuas> _loadedLuaDict = new Dictionary<string, LoadedLuas>();

        public string DefaultInstanceName => _options.DefaultInstanceName ?? _options.ConnectionSettings[0].InstanceName;

        public RedisCache(IOptions<RedisCacheOptions> options, ILogger<RedisCache> logger)
        {
            _logger = logger;
            _options = options.Value;
            _instanceSettingDict = _options.ConnectionSettings.ToDictionary(s => s.InstanceName);

            InitLoadedLuas();
        }

        #region privates

        private string GetRealKey(string key)
        {
            return _options.ApplicationName + key;
        }

        /// <summary>
        /// 各服务器反复Load也没有关系
        /// </summary>
        private void InitLoadedLuas()
        {
            foreach (RedisInstanceSetting setting in _options.ConnectionSettings)
            {
                IServer server = RedisInstanceManager.GetServer(setting);
                LoadedLuas loadedLuas = new LoadedLuas
                {
                    LoadedSetLua = server.ScriptLoad(_luaSet),
                    LoadedGetAndRefreshLua = server.ScriptLoad(_luaGetAndRefresh)
                };

                _loadedLuaDict[setting.InstanceName] = loadedLuas;
            }
        }

        private LoadedLuas GetDefaultLoadLuas()
        {
            return GetLoadedLuas(DefaultInstanceName);
        }

        private LoadedLuas GetLoadedLuas(string instanceName)
        {
            if (_loadedLuaDict.TryGetValue(instanceName, out LoadedLuas loadedLuas))
            {
                return loadedLuas;
            }

            InitLoadedLuas();

            if (_loadedLuaDict.TryGetValue(instanceName, out LoadedLuas loadedLuas2))
            {
                return loadedLuas2;
            }

            throw new CacheException(ErrorCode.CacheLoadedLuaNotFound, $"Can not found LoadedLua Redis Instance: {instanceName}");
        }

        private async Task<IDatabase> GetDatabaseAsync(string? instanceName)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = DefaultInstanceName;
            }

            if (_instanceSettingDict.TryGetValue(instanceName, out RedisInstanceSetting setting))
            {
                return await RedisInstanceManager.GetDatabaseAsync(setting).ConfigureAwait(false);
            }

            throw new CacheException(ErrorCode.CacheLoadedLuaNotFound, $"Can not found Such Redis Instance: {instanceName}");
        }

        private Task<IDatabase> GetDefaultDatabaseAsync()
        {
            return GetDatabaseAsync(DefaultInstanceName);
        }

        private IDatabase GetDatabase(string? instanceName)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = DefaultInstanceName;
            }

            if (_instanceSettingDict.TryGetValue(instanceName, out RedisInstanceSetting setting))
            {
                return RedisInstanceManager.GetDatabase(setting);
            }

            throw new CacheException(ErrorCode.CacheLoadedLuaNotFound, $"Can not found Such Redis Instance: {instanceName}");
        }

        private IDatabase GetDefaultDatabase()
        {
            return GetDatabase(DefaultInstanceName);
        }

        public void Close()
        {
            _instanceSettingDict.ForEach(kv =>
            {
                RedisInstanceManager.Close(kv.Value);
            });
        }

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Entity

        private string GetDimensionKey(string entityName, string dimensionKeyName)
        {
            return GetRealKey(entityName + dimensionKeyName);
        }

        /// <summary>
        /// 返回9 说明，dimension失效，要删除所有dimension
        /// 返回8，说明，找到，但过期，删除所有dimension
        /// 返回data，data[4]=7表示需要更新其他dimension
        /// </summary>
        private const string _luaEntityGetAndRefresh = @"
local guid = redis.call('hget',KEYS[1], ARGV[1])

if (not guid) then
    return nil
end

local data= redis.call('hmget',guid, 'absexp', 'sldexp','data') 

if (not data) then
    redis.call('del', KEYS[1])
    return 9
end

local now = tonumber((redis.call('time'))[1]) 

data[1] = tonumber(data[1])
data[2] = tonumber(data[2])

if(data[1]~= -1 and now >=data[1]) then 
    redis.call('del',KEYS[1])
    redis.call('del',guid)
    return 8 
end 

local curexp=-1 

if(data[1]~=-1 and data[2]~=-1) then 
    curexp=data[1]-now 
    
    if (data[2]<curexp) then 
        curexp=data[2] 
    end 
elseif (data[1]~=-1) then 
    curexp=data[1]-now 
elseif (data[2]~=-1) then 
    curexp=data[2] 
end 

if(curexp~=-1) then 
    redis.call('expire', guid, curexp)
    redis.call('expire', KEYS[1], curexp) 
    data[4]= 7
end 

return data";

        private const string _luaEntitySet = @"
redis.call('hmset', KEYS[1],'absexp',ARGV[1],'sldexp',ARGV[2],'data',ARGV[4]) 

if(ARGV[3]~=-1) then 
    redis.call('expire',KEYS[1], ARGV[3]) 
end ";

        private const string _luaEntityDimensionSetTemplate = @" redis.call('hset', '{0}', '{1}', '{2}') ";

        private const string _luaEntityRemove = @" redis.call('del', KEYS[1]) ";
        private const string _luaEntityDimensionRemoveTemplate = @" redis.call('hdel', '{0}', '{1}')";
        private const string _luaEntityRemoveBy

        public async Task<(TEntity?, bool)> GetEntityAsync<TEntity>(string dimensionKeyName, string dimensionKeyValue, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            if (entityDef.GuidKeyProperty.Name == dimensionKeyName)
            {
                //mean dimensionKeyValue is a guid
                return await GetAsync<TEntity>(dimensionKeyValue, token).ConfigureAwait(false);
            }

            if (!entityDef.OtherDimensions.Any(p => p.Name == dimensionKeyName))
            {
                throw new CacheException(ErrorCode.CacheNoSuchDimensionKey, $"{entityDef.Name}, {dimensionKeyName}");
            }

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);

            RedisResult result = await database.ScriptEvaluateAsync(
                _luaEntityGetAndRefresh,
                new RedisKey[] { GetDimensionKey(entityDef.Name, dimensionKeyName) },
                new RedisValue[] { dimensionKeyValue }).ConfigureAwait(false);

            if (result.IsNull)
            {
                return (null, false);
            }

            RedisResult[]? results = (RedisResult[])result;

            if (results == null)
            {
                return (null, false);
            }

            TEntity? entity = await SerializeUtil.UnPackAsync<TEntity>((byte[])results[2]).ConfigureAwait(false);

            return (entity, true);
        }

        public async Task SetEntityAsync<TEntity>(TEntity entity, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            string guidRealKey = GetRealKey(entityDef.GuidKeyProperty.GetValue(entity).ToString());

            StringBuilder luaBuilder = new StringBuilder(_luaEntitySet);

            foreach (PropertyInfo property in entityDef.OtherDimensions)
            {
                luaBuilder.AppendFormat(CultureInfo.InvariantCulture,
                    _luaEntityDimensionSetTemplate,
                    GetDimensionKey(entityDef.Name, property.Name),
                    property.GetValue(entity),
                    guidRealKey);
            }

            long? absoluteExpireUnixSeconds = entityDef.EntryOptions.AbsoluteExpiration?.ToUnixTimeSeconds();
            long? slideSeconds = (long?)(entityDef.EntryOptions.SlidingExpiration?.TotalSeconds);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);

            byte[] data = await SerializeUtil.PackAsync(entity).ConfigureAwait(false);

            await database.ScriptEvaluateAsync(luaBuilder.ToString(), new RedisKey[] { guidRealKey },
                new RedisValue[]
                {
                    absoluteExpireUnixSeconds??-1,
                    slideSeconds??-1,
                    GetExpireSeconds(absoluteExpireUnixSeconds, slideSeconds)??-1,
                    data
                }).ConfigureAwait(false);
        }

        public async Task RemoveEntityAsync<TEntity>(string dimensionKeyName, string dimensionKeyValue, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);


        }

        public bool IsEnabled<TEntity>() where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            return entityDef.IsCacheable;
        }

        #endregion

        #region Batch Entity

        public async Task<(IEnumerable<TEntity?>, bool)> GetEntitiesAsync<TEntity>(string dimensionKeyName, IEnumerable<string> dimensionKeyValues, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);
        }

        public async Task SetEntitiesAsync<TEntity>(IEnumerable<TEntity> entity, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);
        }

        public async Task SetEntitiesAsync<TEntity>(IEnumerable<string> dimensionKeyNames, IEnumerable<string> dimensionKeyValues, IEnumerable<TEntity?> entities, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);
        }

        public async Task RemoveEntitiesAsync<TEntity>(IEnumerable<string> dimensionKeyNames, IEnumerable<string> dimensionKeyValues, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();
            ThrowIfNotCacheEnabled(entityDef);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);
        }

        private static void ThrowIfNotCacheEnabled(CacheEntityDef entityDef)
        {
            if (!entityDef.IsCacheable)
            {
                throw new CacheException(ErrorCode.CacheNotEnabledForEntity, $"{entityDef.Name}");
            }
        }

        #endregion

    }
}
