using HB.FullStack.Common.Entities;
using HB.FullStack.Database.Entities;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HB.FullStack.Identity.Entities
{
    /// <summary>
    /// 角色
    /// </summary>
    [DatabaseEntity]
    public class Role : Entity
    {
        [EntityProperty("角色名", Unique = true, NotNull = true)]
        public string Name { get; set; } = default!;

        [EntityProperty("DisplayName", Length = 500, NotNull = true)]
        public string DisplayName { get; set; } = default!;

        [EntityProperty("是否激活")]
        public bool IsActivated { get; set; }

        [EntityProperty("说明", Length = 1024)]
        public string? Comment { get; set; }
    }


}