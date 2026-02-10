using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;

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
        public async Task<IActionResult> AllShedules()
        {
            var lessons = await _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.User)
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.Subjects)
                .Include(l => l.ClassGroup)
                .Include(l => l.Room)
                .Include(l => l.TimeSlot)
                .OrderBy(l => l.ClassGroup.Grade)
                    .ThenBy(l => l.ClassGroup.Letter)
                    .ThenBy(l => l.TimeSlot.DayOfWeek)
                    .ThenBy(l => l.TimeSlot.Period)
                    .ToListAsync();

            ViewData["Title"] = "Расписание на полугодие";

            return View(lessons);

        }


    }
}
