﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HB.Framework.Cache;
using HB.Framework.Common.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HB.Infrastructure.Redis.Cache
{
    internal partial class RedisCache
    {
        /// <summary>
        /// keys:guidkey
        /// argv:sldexp
        /// </summary>
        private const string _luaEntityGetAndRefresh = @"
local data = redis.call('hmget', KEYS[1], 'absexp','data','dim')

if (not data) then
    return nil
end

if(data[1]~='-1') then
    local now = tonumber((redis.call('time'))[1])
    local absexp = tonumber(data[1])
    if(now>=absexp) then
        redis.call('del',KEYS[1])
    
        if (data[3]~='') then
            for i in string.gmatch(data[3], '%w+') do
               redis.call('del', i) 
            end
        end
        return nil 
    end
end

if(ARGV[1]~='-1') then
    redis.call('expire', KEYS[1], ARGV[1])
    
    if (data[3]~='') then
        for j in string.gmatch(data[3], '%w+') do
           redis.call('expire', j, ARGV[1]) 
        end
    end
end

return data";

        /// <summary>
        /// KEYS:dimensionKey
        /// ARGV:sldexp
        /// </summary>
        private const string _luaEntityGetAndRefreshByDimension = @"
local guid = redis.call('get',KEYS[1])

if (not guid) then
    return nil
end

local data= redis.call('hmget',guid, 'absexp','data','dim') 

if (not data) then
    redis.call('del', KEYS[1])
    return nil
end

if(data[1]~='-1') then
    local now = tonumber((redis.call('time'))[1])
    local absexp = tonumber(data[1])
    if(now>=absexp) then
        redis.call('del',guid)
    
        if (data[3]~='') then
            for i in string.gmatch(data[3], '%w+') do
               redis.call('del', i) 
            end
        end
        return nil 
    end
end

if(ARGV[1]~='-1') then
    redis.call('expire', guid, ARGV[1])
    
    if (data[3]~='') then
        for j in string.gmatch(data[3], '%w+') do
           redis.call('expire', j, ARGV[1]) 
        end
    end
end

return data";

        /// <summary>
        /// keys: guidKey, dimensionkey1, dimensionkey2, dimensionkey3
        /// argv: absexp_value, expire_value, data_value, dimensionKeyJoinedString, 3(dimensionkey_count)
        /// </summary>
        private const string _luaEntitySet = @"
redis.call('hmset', KEYS[1],'absexp',ARGV[1],'data',ARGV[3], 'dim', ARGV[4]) 

if(ARGV[2]~='-1') then 
    redis.call('expire',KEYS[1], ARGV[2]) 
end 

for i=2, ARGV[5]+1 do
    redis.call('set', KEYS[i], KEYS[1])
    if (ARGV[2]~='-1') then
        redis.call('expire', KEYS[i], ARGV[2])
    end
end";

        /// <summary>
        /// keys: guidKey
        /// argv: 
        /// </summary>
        private const string _luaEntityRemove = @" 
local data=redis.call('hget', KEYS[1], 'dim')

redis.call('del', KEYS[1]) 

if (not data) do
    return 1
end

if(data[1]~='') then
    for i in string.gmatch(data[1], '%w+') do
        redis.call('del', i)
    end
end";

        /// <summary>
        /// keys:dimensionKey
        /// </summary>
        private const string _luaEntityRemoveByDimension = @"
local guid = redis.call('get',KEYS[1])

if (not guid) then
    return nil
end

local data= redis.call('hget',guid, 'dim') 

if (not data) then
    redis.call('del', KEYS[1])
    return 9
end

redis.call('del',guid)
    
if (data[1]~='') then
    for i in string.gmatch(data[1], '%w+') do
        redis.call('del', i) 
    end
end
return 1";

        public async Task<(TEntity?, bool)> GetEntityAsync<TEntity>(string dimensionKeyName, string dimensionKeyValue, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            List<RedisKey> redisKeys = new List<RedisKey>();
            List<RedisValue> redisValues = new List<RedisValue>();

            byte[] loadedScript = AddGetEntityRedisInfo<TEntity>(dimensionKeyName, dimensionKeyValue, entityDef, redisKeys, redisValues);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);

            try
            {
                RedisResult result = await database.ScriptEvaluateAsync(
                    loadedScript,
                    redisKeys.ToArray(),
                    redisValues.ToArray()).ConfigureAwait(false);

                return await MapGetEntityRedisResultAsync<TEntity>(result).ConfigureAwait(false);
            }
            catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.InvariantCulture))
            {
                _logger.LogError(ex, "NOSCRIPT, will try again.");

                InitLoadedLuas();

                return await GetEntityAsync<TEntity>(dimensionKeyName, dimensionKeyValue, token).ConfigureAwait(false);
            }
        }

        public async Task SetEntityAsync<TEntity>(TEntity entity, CancellationToken token = default) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            List<RedisKey> redisKeys = new List<RedisKey>();
            List<RedisValue> redisValues = new List<RedisValue>();

            await AddSetEntityRedisInfoAsync(entity, entityDef, redisKeys, redisValues).ConfigureAwait(false);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);

            try
            {
                await database.ScriptEvaluateAsync(
                    GetLoadedLuas(entityDef.CacheInstanceName!).LoadedEntitySetLua,
                    redisKeys.ToArray(),
                    redisValues.ToArray()).ConfigureAwait(false);
            }
            catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.InvariantCulture))
            {
                _logger.LogError(ex, "NOSCRIPT, will try again.");

                InitLoadedLuas();

                await SetEntityAsync<TEntity>(entity, token).ConfigureAwait(false);
            }
        }

        public async Task RemoveEntityAsync<TEntity>(string dimensionKeyName, string dimensionKeyValue, CancellationToken token = default(CancellationToken)) where TEntity : Entity, new()
        {
            CacheEntityDef entityDef = CacheEntityDefFactory.Get<TEntity>();

            ThrowIfNotCacheEnabled(entityDef);

            List<RedisKey> redisKeys = new List<RedisKey>();

            byte[] loadedScript = AddRemoveEntityRedisInfo<TEntity>(dimensionKeyName, dimensionKeyValue, entityDef, redisKeys);

            IDatabase database = await GetDatabaseAsync(entityDef.CacheInstanceName).ConfigureAwait(false);

            try
            {
                await database.ScriptEvaluateAsync(loadedScript, redisKeys.ToArray()).ConfigureAwait(false);
            }
            catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.InvariantCulture))
            {
                _logger.LogError(ex, "NOSCRIPT, will try again.");

                InitLoadedLuas();

                await RemoveEntityAsync<TEntity>(dimensionKeyName, dimensionKeyValue, token).ConfigureAwait(false);
            }
        }

        private byte[] AddGetEntityRedisInfo<TEntity>(string dimensionKeyName, string dimensionKeyValue, CacheEntityDef entityDef, List<RedisKey> redisKeys, List<RedisValue> redisValues) where TEntity : Entity, new()
        {
            byte[] loadedScript;

            if (entityDef.GuidKeyProperty.Name == dimensionKeyName)
            {
                loadedScript = GetLoadedLuas(entityDef.CacheInstanceName!).LoadedEntityGetAndRefreshLua;
                redisKeys.Add(GetRealKey(dimensionKeyValue));
            }
            else
            {
                ThrowIfNotADimensionKeyName(dimensionKeyName, entityDef);

                loadedScript = GetLoadedLuas(entityDef.CacheInstanceName!).LoadedEntityGetAndRefreshByDimensionLua;
                redisKeys.Add(GetEntityDimensionKey(entityDef.Name, dimensionKeyName, dimensionKeyValue));
            }

            redisValues.Add(entityDef.SlidingTime?.TotalSeconds ?? -1);

            return loadedScript;
        }

        private async Task AddSetEntityRedisInfoAsync<TEntity>(TEntity entity, CacheEntityDef entityDef, List<RedisKey> redisKeys, List<RedisValue> redisValues) where TEntity : Entity, new()
        {
            string guidRealKey = GetRealKey(entityDef.GuidKeyProperty.GetValue(entity).ToString());

            redisKeys.Add(guidRealKey);

            StringBuilder joinedDimensinKeyBuilder = new StringBuilder();

            foreach (PropertyInfo property in entityDef.Dimensions)
            {
                string dimentionKey = GetEntityDimensionKey(entityDef.Name, property.Name, property.GetValue(entity).ToString());
                redisKeys.Add(dimentionKey);
                joinedDimensinKeyBuilder.Append(dimentionKey);
                joinedDimensinKeyBuilder.Append(' ');
            }

            if (joinedDimensinKeyBuilder.Length > 0)
            {
                joinedDimensinKeyBuilder.Remove(joinedDimensinKeyBuilder.Length - 1, 1);
            }

            DateTimeOffset? absulteExpireTime = entityDef.AbsoluteTimeRelativeToNow != null ? DateTimeOffset.UtcNow + entityDef.AbsoluteTimeRelativeToNow : null;
            long? absoluteExpireUnixSeconds = absulteExpireTime?.ToUnixTimeSeconds();
            long? slideSeconds = (long?)(entityDef.SlidingTime?.TotalSeconds);
            long? expireSeconds = GetInitialExpireSeconds(absoluteExpireUnixSeconds, slideSeconds);

            byte[] data = await SerializeUtil.PackAsync(entity).ConfigureAwait(false);


            redisValues.Add(absoluteExpireUnixSeconds ?? -1);
            redisValues.Add(expireSeconds ?? -1);
            redisValues.Add(data);
            redisValues.Add(joinedDimensinKeyBuilder.ToString());
            redisValues.Add(entityDef.Dimensions.Count);
        }

        private byte[] AddRemoveEntityRedisInfo<TEntity>(string dimensionKeyName, string dimensionKeyValue, CacheEntityDef entityDef, List<RedisKey> redisKeys) where TEntity : Entity, new()
        {
            byte[] loadedScript;

            if (entityDef.GuidKeyProperty.Name == dimensionKeyName)
            {
                redisKeys.Add(GetRealKey(dimensionKeyValue));
                loadedScript = GetLoadedLuas(entityDef.CacheInstanceName!).LoadedEntityRemoveLua;
            }
            else
            {
                redisKeys.Add(GetEntityDimensionKey(entityDef.Name, dimensionKeyName, dimensionKeyValue));
                loadedScript = GetLoadedLuas(entityDef.CacheInstanceName!).LoadedEntityRemoveByDimensionLua;
            }

            return loadedScript;
        }
    }
}
