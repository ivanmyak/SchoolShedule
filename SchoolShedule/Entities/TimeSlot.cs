using SchoolShedule.Enums;
using System.ComponentModel;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Учебное занятие 
    /// </summary>
    public class TimeSlot
    {
        public int Id { get; set; }

        /// <summary>
        /// День недели
        /// </summary>
        /// <remarks>Ожидаем, что работать будем только в будние дни, т.е. ПН-ПТ</remarks>
        [DisplayName("День недели")]
        public Day DayOfWeek { get; set; }

        /// <summary>
        /// Номер урока по порядку в данный день
        /// </summary>
        /// <remarks>Ожидаем, что отсчёт будет с 1 урока. Предполагаем, что больше 8 уроков не будет (хотя особо разницы пока не вижу)</remarks>
        [DisplayName("Номер урока")]
        public int Period { get; set; }
    }
}
