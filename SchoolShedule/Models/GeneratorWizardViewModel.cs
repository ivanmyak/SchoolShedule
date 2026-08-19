using SchoolShedule.Entities;
using SchoolShedule.Enums;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    /// <summary>
    /// Модель данных мастера генерации расписания.
    /// </summary>
    public class GeneratorWizardViewModel
    {
        /// <summary>
        /// Выбранный тип периода генерации расписания.
        /// </summary>
        [Required]
        [Display(Name ="Период расписания")]
        public SchedulePeriodType? PeriodType { get; set; }

        public IEnumerable<SchedulePeriodType> Periods =>
            Enum.GetValues(typeof(SchedulePeriodType)).Cast<SchedulePeriodType>();

        /// <summary>
        /// Список идентификаторов выбранных классов/групп.
        /// </summary>
        public List<int> SelectedClassIds { get; set; } = new();

        /// <summary>
        /// Группы классов
        /// </summary>
        public List<ClassGroup> ClassGroups { get; set; } = null!;

        /// <summary>
        /// Список идентификаторов выбранных предметов.
        /// </summary>
        public List<int> SelectedSubjectIds { get; set; } = new();

        /// <summary>
        /// Все предметы для обучения
        /// </summary>
        public List<Subject> Subjects { get; set; } = null!;

        /// <summary>
        /// Сопоставление предмета и списка выбранных преподавателей.
        /// </summary>
        public Dictionary<int, List<Guid>> SelectedTeachers { get; set; } = new();

        /// <summary>
        /// Все учителя школы
        /// </summary>
        public List<Teacher> Teachers { get; set; } = null!;

        /// <summary>
        /// Идентификатор шаблона генерации расписания, если он уже создан.
        /// </summary>
        public int? TemplateId { get; set; }
    }
}
