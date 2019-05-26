using HB.Framework.Database.Entity;
using System;
using System.ComponentModel.DataAnnotations;

namespace HB.Component.Identity.Entity
{
    /// <summary>
    /// ��ɫ
    /// </summary>
    public class Role : DatabaseEntity
    {
        [UniqueGuidEntityProperty]
        public string Guid { get; set; }

        [EntityProperty("��ɫ��", Unique = true, NotNull = true)]
        public string Name { get; set; }

        [EntityProperty("DisplayName", Length=500)]
        public string DisplayName { get; set; }

        [EntityProperty("�Ƿ񼤻�")]
        public bool IsActivated { get; set; }

        [EntityProperty("˵��", Length=1024)]
        public string Comment { get; set; }
    }

    
}