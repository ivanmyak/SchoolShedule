using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(ClassIndex));
        }

        [AllowAnonymous]
        public async Task<IActionResult> ClassIndex()
        {
            var classes = await _context.ClassGroups
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Letter)
                .ToListAsync();

            ViewData["Title"] = $"Выбор класса для расписания";
            return View(classes);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        public async Task<IActionResult> TeacherIndex()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .OrderBy(t => t.User.LastName)
                .ToListAsync();

            ViewData["Title"] = $"Выбор учителя для расписания";
            return View(teachers);
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
        /// <param name="classId">Класс, чьё расписание мы желаем</param>
        public async Task<IActionResult> ClassShedule(int? classId)
        {
            if (classId is null)
                return BadRequest("Класс не выбран.");

            var classGroup = await _context.ClassGroups
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classGroup is null)
                return NotFound("Класс не найден.");

            var lessons = await _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.Teacher).ThenInclude(t => t.User)
                .Include(l => l.Teacher).ThenInclude(t => t.Subjects)
                .Include(l => l.Room)
                .Include(l => l.TimeSlot)
                .Where(l => l.ClassGroupId == classId)
                .OrderBy(l => l.TimeSlot.DayOfWeek)
                .ThenBy(l => l.TimeSlot.Period)
                .ToListAsync();

            if (!lessons.Any())
            {
                ViewData["Title"] = $"Расписание для класса {classGroup.Name}";
                return View("ClassTableEmpty", classGroup);
            }

            var vm = new ClassScheduleViewModel
            {
                SelectedСlassGroup = classGroup,
                Lessons = lessons
            };

            ViewData["Title"] = $"Расписание для класса {classGroup.Name}";
            return View(vm);
        }

        /// <summary> 
        /// Расписание по преподавателю на четверть
        /// </summary> 
        /// <param name="teacherId">Преподаватель, чьё расписание мы желаем</param>
        public async Task<IActionResult> TeacherShedule(Guid? teacherId)
        {
            if (teacherId is null)
                return BadRequest("Учитель не выбран.");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(t => t.UserId == teacherId);

            if (teacher is null)
                return NotFound("Учитель не найден.");

            var lessons = await _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.ClassGroup)
                .Include(l => l.Room)
                .Include(l => l.TimeSlot)
                .Where(l => l.TeacherId == teacherId)
                .OrderBy(l => l.TimeSlot.DayOfWeek)
                .ThenBy(l => l.TimeSlot.Period)
                .ToListAsync();

            if (!lessons.Any())
                return NotFound($"У учителя {teacher.TeacherString} не найдено расписание...");

            var vm = new TeacherScheduleViewModel
            {
                Teacher = teacher,
                Lessons = lessons
            };
            ViewData["Title"] = $"Расписание {teacher.User.GetFIO}";

            return View(vm);
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

            ViewData["Title"] = "Расписание Предметов";
            AllSchedulesViewModel model = new() { Lessons = lessons, SelectedGroupType = groupType };
            return View(model);
        }


    }
}
