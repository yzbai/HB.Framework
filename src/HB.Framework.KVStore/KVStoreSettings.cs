﻿using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HB.Framework.KVStore
{
    public class KVStoreEntitySchema
    {
        [DisallowNull, NotNull]
        public string? EntityTypeFullName { get; set; }

        [DisallowNull, NotNull]
        public string? InstanceName { get; set; }
        
        public string? Description { get; set; }
    }

    public class KVStoreSettings
    {
        public IList<string> AssembliesIncludeEntity { get; } = new List<string>();

        public IList<KVStoreEntitySchema> KVStoreEntities { get; } = new List<KVStoreEntitySchema>();

    }
}