using SchoolShedule.Entities;
using SchoolShedule.Enums;
namespace SchoolShedule.Models
{

    public class LessonCreateViewModel
    {
        // --------------------------
        // 1. Предзаполняемые значения
        // --------------------------

        public Day? SelectedDay { get; set; }
        public int? SelectedPeriod { get; set; }
        public int? SelectedSubjectId { get; set; }
        public int? SelectedFloor => Rooms?.FirstOrDefault(r => r.Id == SelectedRoomId)?.Floor;
        public int? SelectedNumber => Rooms?.FirstOrDefault(r => r.Id == SelectedRoomId)?.Number;
        public int? SelectedRoomId { get; set; }

        public int? SelectedClassGroupId { get; set; }

        public char? SelectedLetter { get; set; }
        public int? SelectedGrade { get; set; }

        public Guid? SelectedTeacherId { get; set; }

        // --------------------------
        // 2. Списки для выпадающих списков
        // --------------------------

        public List<Teacher> Teachers { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<ClassGroup> ClassGroups { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();

        // --------------------------
        // 3. Удобные свойства
        // --------------------------

        public IEnumerable<Day> Days =>
            Enum.GetValues(typeof(Day)).Cast<Day>();

        public IEnumerable<int> Periods =>
            Enumerable.Range(1, 8);

        // --------------------------
        // 4. Логика для UI
        // --------------------------

        /// <summary>
        /// Возвращает преподавателей, у которых предмет совпадает с выбранным.
        /// Используется для сортировки: сначала профильные, потом остальные.
        /// </summary>
        public IEnumerable<Teacher> GetTeachersSorted()
        {
            if (SelectedSubjectId is null)
                return Teachers.OrderBy(t => t.User.LastName);

            return Teachers
                .OrderByDescending(t => t.Subjects.Any(s => s.Id == SelectedSubjectId))
                .ThenBy(t => t.User.LastName);
        }

        /// <summary>
        /// Возвращает предметы: сначала профильные для выбранного преподавателя.
        /// </summary>
        public IEnumerable<Subject> GetSubjectsSorted()
        {
            if (SelectedTeacherId is null)
                return Subjects.OrderBy(s => s.Name);

            var teacher = Teachers.FirstOrDefault(t => t.UserId == SelectedTeacherId);
            if (teacher == null)
                return Subjects.OrderBy(s => s.Name);

            return Subjects
                .OrderByDescending(s => teacher.Subjects.Any(ts => ts.Id == s.Id))
                .ThenBy(s => s.Name);
        }

        /// <summary>
        /// Сортировка кабинетов: сначала по этажу, затем по номеру.
        /// </summary>
        public IEnumerable<Room> GetRoomsSorted() =>
            Rooms.OrderBy(r => r.Floor).ThenBy(r => r.Number);

        /// <summary>
        /// Сортировка классов: 1А, 1Б, 2А, 2Б...
        /// </summary>
        public IEnumerable<ClassGroup> GetClassGroupsSorted() =>
            ClassGroups.OrderBy(c => c.Grade).ThenBy(c => c.Letter);
    }
}

