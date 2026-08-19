using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        [ForeignKey(name: nameof(User))]
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
        public string TeacherString => $"{User.GetFIO} ({SubjectsToString})";

        /// <summary>
        /// Приоритетные кабинеты для Учителя
        /// </summary>
        [Display(Name = "Кабинеты учителя")]
        public List<Room> AssignedRooms { get; set; } = new();

        /// <summary>
        /// Сборное название основных кабинетов Учителя
        /// </summary>
        /// <example>1-15 / 2-13</example>
        [Display(Name = "Предметы")]
        public string RoomsToString =>
    string.Join(" / ", AssignedRooms.Select(r => r.RoomNumber));
    }

}

