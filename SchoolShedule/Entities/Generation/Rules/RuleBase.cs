namespace SchoolShedule.Entities.Generation.Rules
{
    /// <summary>
    /// Базовый класс для всех правил генерации расписания.
    /// </summary>
    public abstract class RuleBase
    {
        /// <summary>
        /// Уникальный идентификатор правила внутри шаблона.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Человекочитаемое название правила (отображается в UI).
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Проверяет корректность заполнения полей правила.
        /// </summary>
        public abstract IEnumerable<string> Validate();
    }
}
