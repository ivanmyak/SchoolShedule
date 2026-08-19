using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolShedule.Entities.Generation
{
    /// <summary>
    /// Контейнер правил генерации для конкретного предмета и класса в рамках шаблона.
    /// </summary>
    public class LessonRuleContainer
    {
        /// <summary>
        /// Идентификатор контейнера правил.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор шаблона расписания, к которому относится набор правил.
        /// </summary>
        [Required]
        [ForeignKey(name: nameof(Template))]
        public int TemplateId { get; set; }

        /// <summary>
        /// Шаблон расписания, к которому относится набор правил.
        /// </summary>
        public ScheduleTemplate Template { get; set; } = default!;

        /// <summary>
        /// Идентификатор предмета, для которого применяются правила.
        /// </summary>
        [Required]
        [ForeignKey(name: nameof(Subject))]
        public int SubjectId { get; set; }

        /// <summary>
        /// Предмет, для которого применяются правила.
        /// </summary>
        public Subject Subject { get; set; } = default!;

        /// <summary>
        /// Идентификатор класса/группы, для которого применяются правила.
        /// </summary>
        [Required]
        [ForeignKey(name: nameof(ClassGroup))]
        public int ClassGroupId { get; set; }

        /// <summary>
        /// Класс/группа, для которого применяются правила.
        /// </summary>
        public ClassGroup ClassGroup { get; set; } = default!;

        /// <summary>
        /// JSON-представление списка правил (каждое правило — наследник RuleBase).
        /// </summary>
        [Required]
        public string RulesJson { get; set; } = string.Empty;
    }
}
