namespace SchoolShedule.Services.Generation
{
    /// <summary>
    /// Описание конфликта в сгенерированном расписании.
    /// </summary>
    public class ScheduleConflict
    {
        /// <summary>
        /// Человекочитаемое описание конфликта.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор класса/группы, к которому относится конфликт (если применимо).
        /// </summary>
        public int? ClassGroupId { get; set; }

        /// <summary>
        /// Идентификатор предмета, к которому относится конфликт (если применимо).
        /// </summary>
        public int? SubjectId { get; set; }

        /// <summary>
        /// Идентификатор временного слота, в котором обнаружен конфликт (если применимо).
        /// </summary>
        public int? TimeSlotId { get; set; }
    }
}
