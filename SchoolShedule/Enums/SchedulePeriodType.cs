using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Enums
{
    /// <summary>
    /// Тип периода расписания.
    /// </summary>
    public enum SchedulePeriodType
    {
        /// <summary>
        /// Расписание на одну неделю.
        /// </summary>
        [Display(Name = "Неделя")]
        Week = 0,

        /// <summary>
        /// Расписание на четверть.
        /// </summary>
        [Display(Name = "Четверть")]
        Term = 1,

        /// <summary>
        /// Расписание на полугодие.
        /// </summary>
        [Display(Name = "Полугодие")]
        HalfYear = 2,

        /// <summary>
        /// Расписание на учебный год.
        /// </summary>
        [Display(Name = "Год")]
        Year = 3
    }
}
