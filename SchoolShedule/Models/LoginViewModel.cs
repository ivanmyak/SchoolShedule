using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;
    }
}
