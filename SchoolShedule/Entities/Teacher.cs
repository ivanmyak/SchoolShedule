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

        [Display(Name = "Учитель")]
        public User User { get; set; } = default!;

        /// <summary>
        /// Предмет(или несколько), который является основным для преподавателя
        /// </summary>
        [Required]
        [Display(Name = "Предметы")]
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        /// <summary>
        /// Сборная строка Фамилия И.О.
        /// </summary>
        /// <example>Иванов Иван Иванович -> Иванов И.И.
        /// Станиславская Инга -> Станиславская И.</example>
        [Display(Name = "Фамилия И.О.")]
        public string GetFIO =>
    $"{User.LastName} {User.FirstName[0]}." +
    (string.IsNullOrWhiteSpace(User.MiddleName) ? "" : $"{User.MiddleName[0]}.");

        /// <summary>
        /// Сборное название основных предметов Учителя
        /// </summary>
        /// <example>Русская литература / Русский язык</example>
        [Display(Name = "Предметы")]
        public string SubjectsToString =>
    string.Join(" / ", Subjects.Select(s => s.Name));

        /// <summary>
        /// Учитель и его предметы
        /// </summary>
        /// <example> Понедельник, 3 урок -> 1-3</example>
        [Required]
        [Display(Name = "Учитель и его направление")]
        public string TeacherString => $"{GetFIO} ({SubjectsToString})";

    }

}

