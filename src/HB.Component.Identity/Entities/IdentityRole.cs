using HB.Framework.Common.Entities;
using HB.Framework.Database.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HB.Component.Identity.Entities
{
    /// <summary>
    /// ��ɫ
    /// </summary>
    public abstract class IdentityRole : Entity
    {
        [EntityProperty("��ɫ��", Unique = true, NotNull = true)]
        public string Name { get; set; } = default!;

        [EntityProperty("DisplayName", Length = 500, NotNull = true)]
        public string DisplayName { get; set; } = default!;

        [EntityProperty("�Ƿ񼤻�")]
        public bool IsActivated { get; set; }

        [EntityProperty("˵��", Length = 1024)]
        public string? Comment { get; set; }
    }


}