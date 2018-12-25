﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HB.Framework.Common;
using HB.Framework.EventBus.Abstractions;
using HB.Infrastructure.Redis.DuplicateCheck;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HB.Infrastructure.Redis.EventBus
{
    /// <summary>
    /// TODO: 未来使用多线程, 对于_consumeTask 和 _historyTask
    /// </summary>
    public class ConsumeTaskManager : IDisposable
    {
        private const int CONSUME_INTERVAL_SECONDS = 5;

        private string _instanceName;
        private string _eventType;
        private ILogger _logger;
        private IRedisInstanceManager _instanceManager;
        private RedisInstanceSetting _instanceSetting;
        private IEventHandler _eventHandler;

        private Task _consumeTask;
        private CancellationTokenSource _consumeTaskCTS;

        private Task _historyTask;
        private CancellationTokenSource _historyTaskCTS;

        private DuplicateChecker _duplicateChecker;

        public ConsumeTaskManager(
            string brokerName, 
            IRedisInstanceManager instanceManager, 
            string eventType, 
            IEventHandler eventHandler, 
            ILogger consumeTaskManagerLogger)
        {
            _instanceName = brokerName;
            _instanceManager = instanceManager;
            _instanceSetting = _instanceManager.GetInstanceSetting(brokerName);
            _eventType = eventType;
            _eventHandler = eventHandler;
            _logger = consumeTaskManagerLogger;

            _consumeTaskCTS = new CancellationTokenSource();
            _consumeTask = new Task(CosumeTaskProcedure, _consumeTaskCTS.Token, TaskCreationOptions.LongRunning);

            _historyTaskCTS = new CancellationTokenSource();
            _historyTask = new Task(HistoryTaskProcedure, _historyTaskCTS.Token, TaskCreationOptions.LongRunning);

            _duplicateChecker = new DuplicateChecker(_instanceManager, _instanceName, _instanceSetting.EventBusEventMessageExpiredHours * 60 * 60);
        }

        private void HistoryTaskProcedure()
        {
            
        }

        private void CosumeTaskProcedure()
        {
            while (true)
            {
                //1, Get Entity
                IDatabase database = _instanceManager.GetDatabase(_instanceName);

                RedisValue redisValue = database.ListRightPopLeftPush(RedisEventBusEngine.QueueName(_eventType), RedisEventBusEngine.HistoryQueueName(_eventType));

                if (redisValue.IsNullOrEmpty)
                {
                    _logger.LogTrace($"ConsumeTask Sleep, brokerName:{_instanceName}, eventType:{_eventType}");

                    Thread.Sleep(CONSUME_INTERVAL_SECONDS * 1000);

                    continue;
                }

                EventMessageEntity entity = DataConverter.To<EventMessageEntity>(redisValue);

                //2, 过期检查

                double spendHours = (DataConverter.CurrentTimestampSeconds() - entity.Timestamp) / 3600;

                if (spendHours > _instanceSetting.EventBusEventMessageExpiredHours)
                {
                    _logger.LogCritical($"有EventMessage过期，eventType:{_eventType}, entity:{DataConverter.ToJson(entity)}");
                    continue;
                }

                //3, 防重检查

                string AcksSetName = RedisEventBusEngine.AcksSetName(_eventType);
                string token = string.Empty;

                if (!_duplicateChecker.Lock(AcksSetName, entity.Id, out token))
                {
                    //竟然有人在检查entity.Id,好了，这下肯定有人在处理了，任务结束。哪怕那个人没处理成功，也没事，等着history吧。
                    continue;  
                }

                bool? isExist = _duplicateChecker.IsExist(AcksSetName, entity.Id, token);

                if (isExist == null || isExist.Value)
                {
                    _logger.LogInformation($"有EventMessage重复，eventType:{_eventType}, entity:{DataConverter.ToJson(entity)}");

                    _duplicateChecker.Release(AcksSetName, entity.Id, token);

                    continue;
                }

                //4, Handle Entity
                try
                {
                    _eventHandler.Handle(entity.JsonData);
                }
                catch(Exception ex)
                {
                    _logger.LogCritical(ex, $"处理消息出错, eventType:{_eventType}, entity : {DataConverter.ToJson(entity)}");
                }

                //5, Acks
                _duplicateChecker.Add(AcksSetName, entity.Id, entity.Timestamp, token);
                _duplicateChecker.Release(AcksSetName, entity.Id, token);
            }
        }

        public void Cancel()
        {
            _consumeTaskCTS.Cancel();
        }

        public void Start()
        {
            _consumeTask.Start(TaskScheduler.Default);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _consumeTaskCTS?.Cancel();
                    _consumeTaskCTS.Dispose();
                    _consumeTask.Dispose();

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~ConsumeTaskManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}