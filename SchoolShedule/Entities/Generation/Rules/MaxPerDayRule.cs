namespace SchoolShedule.Entities.Generation.Rules
{
    /// <summary>
    /// Ограничение на максимальное количество уроков предмета в день.
    /// </summary>
    public class MaxPerDayRule : RuleBase
    {
        public override string DisplayName => "Максимум уроков в день";

        public int MaxLessons { get; set; } = 1;

        public override IEnumerable<string> Validate()
        {
            if (MaxLessons < 1)
                yield return "Максимум уроков должен быть >= 1.";
        }
    }
}
