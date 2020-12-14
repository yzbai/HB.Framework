using HB.FullStack.Common.Entities;

namespace HB.FullStack.Identity.Entities
{
    /// <summary>
    /// ��ɫ
    /// </summary>
    [DatabaseEntity]
    public class Role : Entity
    {
        [EntityProperty(Unique = true, NotNull = true)]
        public string Name { get; set; } = default!;

        [EntityProperty(MaxLength = 500, NotNull = true)]
        public string DisplayName { get; set; } = default!;

        [EntityProperty]
        public bool IsActivated { get; set; }

        [EntityProperty(MaxLength = 1024)]
        public string? Comment { get; set; }
    }


}