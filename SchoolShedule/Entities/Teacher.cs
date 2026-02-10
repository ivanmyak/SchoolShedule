using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Metrics;

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
        /// Предмет(или несколько), который является основным для преподавателя
        /// </summary>
        [Required]
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        /// <summary>
        /// Сборное название основных предметов Учителя
        /// </summary>
        /// <example>Русская литература / Русский язык</example>
        public string SubjectsToString =>
    string.Join("/ ", Subjects.Select(s => s.Name));
    }

}

