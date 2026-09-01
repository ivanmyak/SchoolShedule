using SchoolShedule.Entities;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    public class TeacherUpdateViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Введите Имя для учителя")]
        [MaxLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = default!;

        [Required(ErrorMessage = "Введите Фамилию для учителя")]
        [MaxLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = default!;

        [MaxLength(50)]
        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email (логин)")]
        public string Email { get; set; } = default!;

        [Required]
        [Display(Name = "Основные предметы")]
        public List<int> SelectedSubjectIds { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();

        [Required]
        [Display(Name = "Основные кабинеты")]
        public List<int> SelectedRoomIds { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();

    }
}
