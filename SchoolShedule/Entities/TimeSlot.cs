using SchoolShedule.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Учебное занятие (только параметры периода - более подробно - в <see cref="Lesson"/>)
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

        /// <summary>
        /// Позиция/ячейка урока
        /// </summary>
        /// <example> Понедельник, 3 урок -> 1-3</example>
        [Required]
        [Display(Name = "Номер кабинета")]
        public string TimeSlotString => $"{(int)DayOfWeek + 1}-{Period}";
    }
}
