using Microsoft.AspNetCore.Identity;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;

namespace SchoolShedule.Services
{
    /// <summary>
    /// Сервис-генератор 
    /// Логина, 
    /// Email, 
    /// Пароля
    /// </summary>
    public class CredentialsGeneratorService : ICredentialsGenerator
    {
        private readonly UserManager<User> _userManager;
        private readonly ITranslit _translit;

        public CredentialsGeneratorService(UserManager<User> userManager, ITranslit translit)
        {
            _userManager = userManager;
            _translit = translit;
        }

        /// <summary>
        /// Генерация Логина (UserName)
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        public async Task<string> GenerateUserNameAsync(string firstName, string lastName)
        {
            string baseName = _translit.Normalize(
                $"{_translit.Transliterate(firstName)}.{_translit.Transliterate(lastName)}"
            );

            string userName = baseName;
            int counter = 2;

            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{baseName}{counter}";
                counter++;
            }

            return userName;
        }

        public async Task<string> GenerateEmailAsync(string userName)
        {
            string baseEmail = $"{userName}@teachermail.com";
            string email = baseEmail;
            int counter = 2;

            while (await _userManager.FindByEmailAsync(email) != null)
            {
                email = $"{userName}{counter}@teachermail.com";
                counter++;
            }

            return email;
        }

        public string GeneratePassword(string firstName, string lastName)
        {
            return $"{_translit.Transliterate(firstName)}_Tea2!$3cH_{_translit.Transliterate(lastName)}";
        }
    }
}
