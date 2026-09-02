using SchoolShedule.Entities;
using SchoolShedule.Enums;

namespace SchoolShedule.Models
{
    public class ManualGenerateViewModel
    {
        // Класс, по которому происхдит генерация
        public ClassGroup ClassGroup { get; set; } = null!;

        public List<Teacher> Teachers { get; set; } = null!;

        public List<Room> Rooms { get; set; } = null!;

        public List<Subject> Subjects { get; set; } = null!;

        public SchedulePeriodType SelectedSchedulePeriod { get; set; } = SchedulePeriodType.Week;

        /// <summary>
        /// Расчитанная нагрузка в виде уже готовых для заполнения, слотов
        /// </summary>
        public List<TimeSlot> CalculatedTimeSlots { get; set; } = null!;

        /// <summary>
        /// ID Предмета и необходимое число часов в выбранный период
        /// </summary>
        public List<(int, int)> SubjectHours { get; set; } = new();

    }
}
