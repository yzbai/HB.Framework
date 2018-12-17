﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HB.Framework.EventBus.Abstractions
{
    public interface IEventHandler : IDisposable
    {
        string Id { get; set; }

        string EventType { get; set; }

        void Handle(byte[] body);
    }
}
