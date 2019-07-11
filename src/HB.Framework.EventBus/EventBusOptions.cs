﻿using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HB.Framework.EventBus
{
    public class EventSchema
    {
        public string EventType { get; set; }

        public string BrokerName { get; set; }

    }

    public class EventBusOptions : IOptions<EventBusOptions>
    {
        public EventBusOptions Value {
            get {
                return this;
            }
        }

        public IList<EventSchema> EventSchemas { get; } = new List<EventSchema>();

        public EventSchema GetEventSchema(string topic)
        {
            return EventSchemas.FirstOrDefault(t => t.EventType.Equals(topic, GlobalSettings.Comparison));
        }
    }
}