namespace SchoolShedule.Entities.Generation.Rules
{
    /// <summary>
    /// Правило, определяющее список кабинетов, в которых разрешено проводить предмет.
    /// </summary>
    public class AllowedRoomsRule : RuleBase
    {
        public override string DisplayName => "Специализированные кабинеты";

        /// <summary>
        /// Список разрешённых кабинетов.
        /// </summary>
        public List<int> RoomIds { get; set; } = new();

        public override IEnumerable<string> Validate()
        {
            if (!RoomIds.Any())
                yield return "Необходимо выбрать хотя бы один кабинет.";
        }
    }
}
