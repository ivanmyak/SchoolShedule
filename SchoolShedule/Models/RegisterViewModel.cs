using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    public class RegisterViewModel
    {
        [Required, EmailAddress(ErrorMessage = "Некорректный формат email!")]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Пароли не совпадают!")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
