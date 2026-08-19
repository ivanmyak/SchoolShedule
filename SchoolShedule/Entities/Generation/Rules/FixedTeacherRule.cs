namespace SchoolShedule.Entities.Generation.Rules
{
    /// <summary>
    /// Жёсткая привязка преподавателя к классу для предмета.
    /// </summary>
    public class FixedTeacherRule : RuleBase
    {
        public override string DisplayName => "Фиксированный преподаватель";

        public Dictionary<int, Guid> ClassTeacherMap { get; set; } = new();

        public override IEnumerable<string> Validate()
        {
            if (!ClassTeacherMap.Any())
                yield return "Необходимо выбрать хотя бы одну привязку класс-преподаватель.";
        }
    }
}
