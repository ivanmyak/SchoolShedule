using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Учитель
    /// </summary>
    public class Teacher
    {
        [Key]
        public Guid UserId { get; set; }

        public User User { get; set; } = default!;

        /// <summary>
        /// Предмет, который является основным для преподавателя
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Specialization { get; set; } = default!;
    }

}

