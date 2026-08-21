using SchoolShedule.Enums;

namespace SchoolShedule.Models
{
    public class WizardHoursDto
    {
        /// <summary>
        /// Выбранный тип периода.
        /// </summary>
        public SchedulePeriodType PeriodType { get; set; }

        /// <summary>
        /// Ячейки таблицы "предмет × класс".
        /// </summary>
        public List<WizardHoursCellDto> Cells { get; set; } = new();
    }

    /// <summary>
    /// Одна ячейка таблицы "предмет × класс".
    /// </summary>
    public class WizardHoursCellDto
    {
        /// <summary>
        /// Идентификатор предмета.
        /// </summary>
        public int SubjectId { get; set; }

        /// <summary>
        /// Идентификатор класса.
        /// </summary>
        public int ClassGroupId { get; set; }

        /// <summary>
        /// Количество часов предмета для класса.
        /// </summary>
        public int Hours { get; set; }
    }
}
