using SchoolShedule.Entities;
using SchoolShedule.Enums;

namespace SchoolShedule.Models
{
    public class ClassScheduleViewModel
    {
        public ClassGroup SelectedСlassGroup { get; set; } = default!;
        public required IEnumerable<Lesson> Lessons { get; set; }
    }
}
