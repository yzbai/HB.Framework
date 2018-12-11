﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HB.Framework.DistributedQueue
{
    public interface IDistributedQueue
    {
        Task<IDistributedQueueResult> Push<T>(string queueName, T data);
        ulong Length(string queueName);
        IDistributedQueueResult PopAndPush<T>(string fromQueueName, string toQueueName);
    }
}
