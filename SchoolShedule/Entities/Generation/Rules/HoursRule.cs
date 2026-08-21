using SchoolShedule.Enums;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Entities.Generation.Rules
{
    /// <summary>
    /// Правило, определяющее количество часов предмета в выбранный период.
    /// </summary>
    public class HoursRule : RuleBase
    {
        public override string DisplayName => "Количество часов";

        /// <summary>
        /// Количество часов предмета.
        /// </summary>
        [Range(0, 100)]
        public int Hours { get; set; } = 0;

        /// <summary>
        /// Тип периода, к которому относится количество часов.
        /// </summary>
        public SchedulePeriodType PeriodType { get; set; }

        /// <summary>
        /// Идентификатор предмета, к которому применяется правило.
        /// </summary>
        public int SubjectId { get; set; }

        /// <summary>
        /// Идентификатор класса, к которому применяется правило.
        /// Если не указан, правило может применяться к параллели или ко всем выбранным классам.
        /// </summary>
        public int? ClassGroupId { get; set; }

        public override IEnumerable<string> Validate()
        {
            if (Hours < 0)
                yield return "Количество часов должно быть не меньше нуля.";
        }
    }
}
