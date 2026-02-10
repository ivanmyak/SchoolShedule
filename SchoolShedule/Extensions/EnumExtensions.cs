using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SchoolShedule.Extensions
{
    public static class EnumExtensions
    {
        /// <summary> 
        ///Возвращает значение атрибута Display(Name) для enum 
        ///</summary>
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }
    }


}
