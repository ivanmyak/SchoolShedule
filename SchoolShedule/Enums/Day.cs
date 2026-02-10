using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Enums
{
    /// <summary>
    /// Рабочие дни недели
    /// </summary>
    public enum Day
    {
        [Display(Name = "Понедельник")]
        Monday = 1,

        [Display(Name = "Вторник")]
        Tuesday,

        [Display(Name = "Среда")]
        Wednesday,

        [Display(Name = "Четверг")]
        Thursday,

        [Display(Name = "Пятница")]
        Friday
    }
}
