﻿using HB.Framework.DocumentStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace HB.Infrastructure.MongoDB
{
    public class DefaultMongoDB : IMongoDB
    {
        private readonly DocumentStoreOptions _documentOptions;
        private readonly MongoDBOptions _mongoOptions;

        private readonly ILogger _logger;

        /// <summary>
        /// key:InstanceName
        /// </summary>
        private readonly IDictionary<string, IMongoClient> _clientDict;

        /// <summary>
        /// key:InstanceName_DatabaseName
        /// </summary>
        private readonly IDictionary<string, IMongoDatabase> _dbDict;

        /// <summary>
        /// key:InstanceName_DatabaseName_CollectionName
        /// </summary>
        private readonly IDictionary<string, object> _collectionDict;

        public DefaultMongoDB(IOptions<DocumentStoreOptions> documentOptions, IOptions<MongoDBOptions> mongoOptions, ILogger<DefaultMongoDB> logger)
        {
            _logger = logger;

            _documentOptions = documentOptions.Value;
            _mongoOptions = mongoOptions.Value;

            _clientDict = new Dictionary<string, IMongoClient>();
            _dbDict = new Dictionary<string, IMongoDatabase>();
            _collectionDict = new Dictionary<string, object>();
        }

        private IMongoClient GetClient(string instanceName)
        {
            if (_clientDict.ContainsKey(instanceName))
            {
                return _clientDict[instanceName];
            }

            string connectionString = _mongoOptions.GetConnectionString(instanceName);

            MongoClientSettings clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            IMongoClient mongoClient = new MongoClient(clientSettings);

            //TODO: More MongoClientSettings Detail
            //TODO: should retry?

            if (mongoClient != null)
            {
                _clientDict[instanceName] = mongoClient;
            }

            return mongoClient;
        }

        private IMongoDatabase GetDatabase(string instanceName, string databaseName)
        {
            string key = instanceName + "_" + databaseName;

            if (_dbDict.ContainsKey(key))
            {
                return _dbDict[key];
            }

            IMongoClient client = GetClient(instanceName);

            if (client == null)
            {
                //TODO: add log
                return null;
            }

            IMongoDatabase database = client.GetDatabase(databaseName);

            if (database != null)
            {
                _dbDict[key] = database;
            }

            return database;
        }

        public IMongoCollection<T> GetCollection<T>() where T : DocumentStoreEntity, new()
        {
            Type type = typeof(T);

            DocumentStoreSchema schema = _documentOptions.GetDocumentStoreSchema(type);

            string key = schema.InstanceName + "_" + schema.Database + "_" + schema.CollectionName;

            if (_collectionDict.ContainsKey(key))
            {
                return _collectionDict[key] as IMongoCollection<T>;
            }

            IMongoDatabase db = GetDatabase(schema.InstanceName, schema.Database);

            if (db == null)
            {
                //TODO: add log
                return null;
            }

            IMongoCollection<T> collection = db.GetCollection<T>(schema.CollectionName);

            if (collection != null)
            {
                _collectionDict[key] = collection;
            }

            return collection;
        }

        
    }
}
