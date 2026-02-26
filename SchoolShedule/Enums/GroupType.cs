using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Enums
{
    /// <summary>
    /// Тип группы классов
    /// </summary>
    public enum GroupType
    {
        //Сборная школа (5-11 классы)
        [Display(Name = "Сборная")]
        Standart = 0,

        //Начальная школа (1-4 классы)
        [Display(Name = "Начальная")]
        Primary = 1,

        //Начальная школа (5-9 классы)
        [Display(Name = "Средняя")]
        Secondary,

        //Начальная школа (10-11 классы)
        [Display(Name = "Старшая")]
        Hight,
    }
}
