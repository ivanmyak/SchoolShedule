using SchoolShedule.Entities;

namespace SchoolShedule.Models
{
    public class TeacherScheduleViewModel
    {
        public required Teacher Teacher { get; set; }
        public required IEnumerable<Lesson> Lessons { get; set; }
    }

}
