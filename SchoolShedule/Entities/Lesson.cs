using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Урок
    /// </summary>
    public class Lesson
    {
        public int Id { get; set; }

        [ForeignKey(name: nameof(Subject))]
        public int SubjectId { get; set; }

        /// <summary>
        /// Предмет урока
        /// </summary>
        [Display(Name = "Предмет урока")]
        public Subject Subject { get; set; } = default!;

        [ForeignKey(name: nameof(Teacher))]
        public Guid TeacherId { get; set; }

        /// <summary>
        /// Учитель, преподающий предмет
        /// </summary>
        [Required]
        [Display(Name = "Учитель")]
        public Teacher Teacher { get; set; } = default!;

        [ForeignKey(name: nameof(ClassGroup))]
        public int ClassGroupId { get; set; }

        /// <summary>
        /// Класс/Группа, которым преподаётся предмет
        /// </summary>
        [Required]
        [Display(Name = "Класс/Группа")]
        public ClassGroup ClassGroup { get; set; } = default!;

        [ForeignKey(name: nameof(Room))]
        public int RoomId { get; set; }

        /// <summary>
        /// Кабинет для урока
        /// </summary>
        [Display(Name ="Кабинет для урока")]
        public Room Room { get; set; } = default!;

        [ForeignKey(name: nameof(TimeSlot))]
        public int TimeSlotId { get; set; }

        /// <summary>
        /// Время + Дата урока
        /// </summary>
        [Display(Name = "День + Номер урока")]
        public TimeSlot TimeSlot { get; set; } = default!;
    }
}
