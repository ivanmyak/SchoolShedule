using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Enums;
using SchoolShedule.Models;

namespace SchoolShedule.Controllers
{
    public class SheduleController : Controller
    {
        private readonly SheduleDbContext _context;

        public SheduleController(SheduleDbContext context)
        {
            _context = context;
        }

        /// <summary> 
        /// Расписание на полугодие 
        /// </summary> 
        /// <param name="groupType">Расписание какой группы классов мы желаем</param>
        public async Task<IActionResult> All(GroupType groupType = GroupType.Standart)
        {
            IEnumerable<int> gradesRange = groupType switch
            {
                GroupType.Primary => Enumerable.Range(1, 4),   // 1–4
                GroupType.Secondary => Enumerable.Range(5, 5), // 5–9
                GroupType.Hight => Enumerable.Range(10, 2),    // 10–11
                _ => Enumerable.Range(5, 7)                   // если ничего не выбрано — идёт стандарт (5-11 классы)
            };

            var lessons = await _context.Lessons
                            .Include(l => l.Subject)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.User)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.Subjects)
                            .Include(l => l.ClassGroup)
                            .Include(l => l.Room)
                            .Include(l => l.TimeSlot)
                            .Where(l => gradesRange.Contains(l.ClassGroup.Grade))
                            .OrderBy(l => l.ClassGroup.Grade)
                                .ThenBy(l => l.ClassGroup.Letter)
                                .ThenBy(l => l.TimeSlot.DayOfWeek)
                                .ThenBy(l => l.TimeSlot.Period)
                                .ToListAsync();

            ViewData["Title"] = "Расписание на четверть";
            AllSchedulesViewModel model = new() { Lessons = lessons, SelectedGroupType = groupType };
            return View(model);
        }

        /// <summary> 
        /// Расписание по классу на четверть
        /// </summary> 
        /// <param name="classid">Класс, чьё расписание мы желаем</param>
        public async Task<IActionResult> ClassShedule(int classid)
        {
            ClassGroup classGroup = await _context.ClassGroups.FindAsync(classid) ?? throw new Exception("Не удалось найти класс");

            var lessons = await _context.Lessons
                            .Include(l => l.Subject)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.User)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.Subjects)
                            .Include(l => l.ClassGroup)
                            .Include(l => l.Room)
                            .Include(l => l.TimeSlot)
                            .Where(l => l.ClassGroup.Id == classid)
                            .OrderBy(l => l.ClassGroup.Grade)
                                .ThenBy(l => l.ClassGroup.Letter)
                                .ThenBy(l => l.TimeSlot.DayOfWeek)
                                .ThenBy(l => l.TimeSlot.Period)
                                .ToListAsync();

            ViewData["Title"] = $"Расписание по классу {classGroup.Name} на текущую четверть";
            ClassScheduleViewModel model = new() { Lessons = lessons, SelectedСlassGroup = classGroup };
            return View(model);
        }

        /// <summary> 
        /// Расписание по предметам
        /// </summary> 
        /// <param name="groupType">Расписание каждой группы классов</param>
        public async Task<IActionResult> AllSpec(GroupType groupType = GroupType.Standart)
        {
            IEnumerable<int> gradesRange = groupType switch
            {
                GroupType.Primary => Enumerable.Range(1, 4),   // 1–4
                GroupType.Secondary => Enumerable.Range(5, 5), // 5–9
                GroupType.Hight => Enumerable.Range(10, 2),    // 10–11
                _ => Enumerable.Range(5, 7)                   // если ничего не выбрано — идёт стандарт (5-11 классы)
            };

            var lessons = await _context.Lessons
                            .Include(l => l.Subject)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.User)
                            .Include(l => l.Teacher)
                                .ThenInclude(t => t.Subjects)
                            .Include(l => l.ClassGroup)
                            .Include(l => l.Room)
                            .Include(l => l.TimeSlot)
                            .Where(l => gradesRange.Contains(l.ClassGroup.Grade))
                            .OrderBy(l => l.ClassGroup.Grade)
                                .ThenBy(l => l.ClassGroup.Letter)
                                .ThenBy(l => l.TimeSlot.DayOfWeek)
                                .ThenBy(l => l.TimeSlot.Period)
                                .ToListAsync();

            ViewData["Title"] = "Расписание на четверть";
            AllSchedulesViewModel model = new() { Lessons = lessons, SelectedGroupType = groupType };
            return View(model);
        }


    }
}
