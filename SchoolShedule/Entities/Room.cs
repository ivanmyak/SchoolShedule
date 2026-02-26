using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Кабинет для занятий
    /// </summary>
    public class Room
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Этаж
        /// </summary>
        [Display(Name = "Этаж")]
        public int Floor { get; set; }

        /// <summary>
        /// Номер кабинета
        /// </summary>
        [Display(Name = "Номер кабинета")]

        public int Number { get; set; }

        /// <summary>
        /// Номер кабинета (в формате <этаж>-<номер_кабинета>)
        /// </summary>
        [Required]
        [Display(Name = "Номер кабинета")]
        public string RoomNumber => $"{Floor}-{Number}";
    }
}
