using SchoolShedule.Entities;
using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    public class TeacherCreateViewModel
    {
        [Required]
        [Display(Name = "Основные предметы")]
        public List<int> SelectedSubjectIds { get; set; } = new();

        /// <summary>
        /// Имя
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = default!;

        /// <summary>
        /// Фамилия
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = default!;

        /// <summary>
        /// Отчество
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }

        public List<Subject> Subjects { get; set; } = new();

        [Display(Name = "Email (логин)")]
        public string? Email { get; set; }

        [Display(Name = "Пароль")]
        public string? Password { get; set; }

        [Display(Name = "Сгенерировать автоматически")]
        public bool AutoGenerateCredentials { get; set; } = true;
    }
}
