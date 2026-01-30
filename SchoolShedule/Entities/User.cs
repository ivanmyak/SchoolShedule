using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace SchoolShedule.Entities
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Имя
        /// </summary>
        [Required] [MaxLength(50)] 
        public string FirstName { get; set; } = default!;

        /// <summary>
        /// Фамилия
        /// </summary>
        [Required] [MaxLength(50)] 
        public string LastName { get; set; } = default!; 

        /// <summary>
        /// Отчество
        /// </summary>
        [MaxLength(50)] 
        public string? MiddleName { get; set; }

        /// <summary>
        /// Дата и время регистрации пользователя
        /// </summary>
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;


    }
}
