﻿
using HB.FullStack.KVStore.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;


namespace HB.FullStack.KVStore
{
    public interface IKVStore
    {
        Task<T?> GetAsync<T>(string key) where T : KVStoreEntity, new();

        Task<T?> GetAsync<T>(long key) where T : KVStoreEntity, new()
        {
            return GetAsync<T>(key.ToString(CultureInfo.InvariantCulture));
        }

        Task<IEnumerable<T?>> GetAsync<T>(IEnumerable<string> keys) where T : KVStoreEntity, new();


        Task<IEnumerable<T?>> GetAllAsync<T>() where T : KVStoreEntity, new();

        Task AddAsync<T>(T item, string lastUser) where T : KVStoreEntity, new();

        /// <summary>
        /// 反应Version变化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="lastUser"></param>
        /// <returns></returns>
        Task AddAsync<T>(IEnumerable<T> items, string lastUser) where T : KVStoreEntity, new();

        Task UpdateAsync<T>(T item, string lastUser) where T : KVStoreEntity, new();

        /// <summary>
        /// 反应Version变化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="lastUser"></param>
        /// <returns></returns>
        Task UpdateAsync<T>(IEnumerable<T> items, string lastUser) where T : KVStoreEntity, new();

        Task DeleteAsync<T>(string key, int version) where T : KVStoreEntity, new();

        Task DeleteAsync<T>(IEnumerable<string> keys, IEnumerable<int> versions) where T : KVStoreEntity, new();


        Task DeleteAllAsync<T>() where T : KVStoreEntity, new();


        string GetEntityKey<T>(T item) where T : KVStoreEntity, new();
    }
}