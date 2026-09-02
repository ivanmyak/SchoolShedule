namespace SchoolShedule.Models
{
    public class ExcelImportReport
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
    }
}
