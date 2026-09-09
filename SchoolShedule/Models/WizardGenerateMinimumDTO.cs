namespace SchoolShedule.Models
{
    public class WizardGenerateMinimumDTO
    {
        /// <summary>
        /// ID выбранного шаблона
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// ID выбранных классов из шаблона
        /// </summary>
        public List<int> SelectedIds { get; set; } = new();
    }
}
