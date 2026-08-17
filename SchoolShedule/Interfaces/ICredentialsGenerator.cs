using SchoolShedule.Entities;

namespace SchoolShedule.Interfaces
{
    /// <summary>
    /// Генератор реквизитов для входа 
    /// </summary>
    public interface ICredentialsGenerator
    {
        /// <summary>
        /// Генерация Логина
        /// </summary>
        /// <param name="firstName">Имя пользователя</param>
        /// <param name="lastName">Фамилия пользователя</param>
        /// <returns></returns>
        Task<string> GenerateUserNameAsync(string firstName, string lastName);

        /// <summary>
        /// Генерация Email
        /// </summary>
        /// <param name="userName">Логин пользователя</param>
        /// <returns></returns>
        Task<string> GenerateEmailAsync(string userName);

        /// <summary>
        /// Генерация Пароля
        /// </summary>
        /// <param name="firstName">Имя пользователя</param>
        /// <param name="lastName">Фамилия пользователя</param>
        /// <returns></returns>
        string GeneratePassword(string firstName, string lastName);

        /// <summary>
        /// Генерация Пароля
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns></returns>
        string GeneratePassword(User user);
    }
}
