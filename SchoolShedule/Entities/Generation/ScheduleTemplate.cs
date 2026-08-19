using SchoolShedule.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolShedule.Entities.Generation
{
    /// <summary>
    /// Шаблон генерации расписания для выбранного периода и набора классов.
    /// </summary>
    public class ScheduleTemplate
    {
        /// <summary>
        /// Идентификатор шаблона.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Пользовательское название шаблона (задаётся администратором).
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Тип периода, для которого предназначен шаблон (неделя, четверть, полугодие, год).
        /// </summary>
        [Required]
        public SchedulePeriodType PeriodType { get; set; }

        /// <summary>
        /// Классы/группы, для которых применяется шаблон.
        /// Полноценная связь многие‑ко‑многим.
        /// </summary>
        public List<ClassGroup> ClassGroups { get; set; } = new();

        /// <summary>
        /// Набор правил генерации уроков по предметам для данного шаблона.
        /// </summary>
        [NotMapped]
        public List<Rules.RuleBase> LessonRules { get; set; } = new();

        /// <summary>
        /// Дата создания шаблона.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Дата последнего изменения шаблона.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
