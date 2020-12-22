﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HB.FullStack.Common.Entities;

namespace HB.FullStack.Database.Def
{
    public abstract class DatabaseEntity : Entity
    {


        [AutoIncrementPrimaryKey]
        [EntityProperty(0)]
        [CacheKey]
        public long Id { get; internal set; } = -1;

    }
}
