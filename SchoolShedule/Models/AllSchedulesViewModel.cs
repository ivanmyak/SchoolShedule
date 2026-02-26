using SchoolShedule.Entities;
using SchoolShedule.Enums;

namespace SchoolShedule.Models
{
    public class AllSchedulesViewModel
    {
        public GroupType SelectedGroupType { get; set; } = GroupType.Standart;
        public required IEnumerable<Lesson> Lessons { get; set; }
    }
}
