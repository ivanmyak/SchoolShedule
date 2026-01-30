using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Ученик
    /// </summary>
    public class Student
    {
        [Key]
        public Guid UserId { get; set; }

        public User User { get; set; } = default!;

        [ForeignKey(name:nameof(ClassGroup))]
        public int ClassGroupId { get; set; }

        /// <summary>
        /// Группа, к которой принадлежит ученик
        /// </summary>
        public ClassGroup ClassGroup { get; set; } = default!;
    }
}
