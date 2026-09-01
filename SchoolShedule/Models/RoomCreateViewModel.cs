using SchoolShedule.Entities;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    public class RoomCreateViewModel
    {
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

        [Required]
        [Display(Name = "Основные предметы кабинета")]
        public List<int> SelectedSubjectIds { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
    }
}
