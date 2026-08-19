using SchoolShedule.Entities;

namespace SchoolShedule.Services.Generation
{
    /// <summary>
    /// Результат работы генератора расписания.
    /// </summary>
    public class ScheduleGenerationResult
    {
        /// <summary>
        /// Признак успешной генерации без критических конфликтов.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Список сгенерированных уроков (расписание).
        /// </summary>
        public List<Lesson> GeneratedLessons { get; set; } = new();

        /// <summary>
        /// Список обнаруженных конфликтов, требующих внимания администратора.
        /// </summary>
        public List<ScheduleConflict> Conflicts { get; set; } = new();
    }
}
