using HB.FullStack.Common.Entities;
using HB.FullStack.Database.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HB.Component.Identity.Entities
{
    /// <summary>
    /// ��ɫ
    /// </summary>
    [DatabaseEntity]
    public class Role : Entity
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