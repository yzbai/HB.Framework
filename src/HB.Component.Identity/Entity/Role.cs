using HB.Framework.Database.Entity;
using System;
using System.ComponentModel.DataAnnotations;

namespace HB.Component.Identity.Entity
{
    /// <summary>
    /// ��ɫ
    /// </summary>
    [Serializable]
    public class Role : DatabaseEntity
    {
        [Required]
        [DatabaseEntityProperty("��ɫ��", Unique = true, NotNull = true)]
        public string Name { get; set; }

        [DatabaseEntityProperty("DisplayName", Length=500)]
        public string DisplayName { get; set; }

        [DatabaseEntityProperty("�Ƿ񼤻�")]
        public bool IsActivated { get; set; }

        [DatabaseEntityProperty("˵��", Length=1024)]
        public string Comment { get; set; }
    }

    
}