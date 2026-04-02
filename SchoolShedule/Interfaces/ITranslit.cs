namespace SchoolShedule.Interfaces
{
    /// <summary>
    /// Транслитирование
    /// </summary>
    public interface ITranslit
    {
        /// <summary>
        /// Транслитирование рус->eng
        /// </summary>
        /// <param name="text">исходная строка на Русском</param>
        /// <returns>Eng string</returns>
        string Transliterate(string text);

        /// <summary>
        /// Нормализация строки - уборка лишних пустых символов и повторений
        /// </summary>
        /// <param name="text">исходная строка</param>
        /// <returns>нормализованная строка</returns>
        string Normalize(string text);
    }
}
