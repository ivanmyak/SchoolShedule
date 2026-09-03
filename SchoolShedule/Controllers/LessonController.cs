using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Enums;
using SchoolShedule.Extensions;
using SchoolShedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolShedule.Controllers
{
    public class LessonController : Controller
    {
        private readonly SheduleDbContext _context;

        public LessonController(SheduleDbContext context)
        {
            _context = context;
        }

        // GET: Lesson
        public async Task<IActionResult> All()
        {
            ViewData["Title"] = "Уроки";

            var sheduleDbContext = _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.Room)
                .Include(l => l.Teacher).ThenInclude(lt => lt.User)
                .Include(l => l.ClassGroup)
                .Include(l => l.TimeSlot);
            return View(await sheduleDbContext.ToListAsync());
        }

        // GET: Lesson/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.ClassGroup)
                .Include(l => l.Room)
                .Include(l => l.Subject)
                .Include(l => l.Teacher)
                .Include(l => l.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="day"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public IActionResult Create(Day? day, int? period, int? roomId, int? subjectId)
        {
            var vm = new LessonCreateViewModel
            {
                SelectedDay = day,
                SelectedPeriod = period,
                SelectedSubjectId = subjectId
            };

            FillLists(vm);
            vm.SelectedRoomId = roomId;

            ViewData["Title"] = "Новый Урок";
            return View(vm);
        }


        // POST: Lesson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LessonCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {

                ViewData["Title"] = "Новый Урок";
                return View(vm);
            }
            var timeSlot = _context.TimeSlots.FirstOrDefault(t => t.DayOfWeek == vm.SelectedDay && t.Period == vm.SelectedPeriod);
            if (timeSlot == null)
            {
                timeSlot = new TimeSlot
                {
                    DayOfWeek = vm.SelectedDay!.Value,
                    Period = vm.SelectedPeriod!.Value
                };

                _context.TimeSlots.Add(timeSlot);
                await _context.SaveChangesAsync();
            }
            var lesson = new Lesson
            {
                TeacherId = vm.SelectedTeacherId!.Value,
                SubjectId = vm.SelectedSubjectId!.Value,
                RoomId = vm.SelectedRoomId!.Value,
                ClassGroupId = vm.SelectedClassGroupId!.Value,
                TimeSlotId = timeSlot.Id
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("AllSpec", "Shedule");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Teacher).ThenInclude(t => t.User)
                .Include(l => l.Teacher).ThenInclude(t => t.Subjects)
                .Include(l => l.ClassGroup)
                .Include(l => l.Room)
                .Include(l => l.Subject)
                .Include(l => l.TimeSlot)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            var vm = new LessonCreateViewModel
            {
                SelectedDay = lesson.TimeSlot.DayOfWeek,
                SelectedPeriod = lesson.TimeSlot.Period,
                SelectedSubjectId = lesson.SubjectId,
                SelectedTeacherId = lesson.TeacherId,
                SelectedClassGroupId = lesson.ClassGroupId,
                SelectedRoomId = lesson.RoomId
            };

            vm.Teachers = _context.Teachers.Include(t => t.User).Include(t => t.Subjects).ToList();
            vm.Subjects = _context.Subjects.ToList();
            vm.Rooms = _context.Rooms.Include(r => r.AssignedSubjects).ToList();
            vm.ClassGroups = _context.ClassGroups.ToList();
            vm.TimeSlots = _context.TimeSlots.ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LessonCreateViewModel vm)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
                return NotFound($"Не найден урок с текушим ID {id}");

            lesson.SubjectId = vm.SelectedSubjectId!.Value;
            lesson.TeacherId = vm.SelectedTeacherId!.Value;
            lesson.ClassGroupId = vm.SelectedClassGroupId!.Value;
            lesson.RoomId = vm.SelectedRoomId!.Value;

            var slot = _context.TimeSlots
                .First(ts => ts.DayOfWeek == vm.SelectedDay && ts.Period == vm.SelectedPeriod);

            lesson.TimeSlotId = slot.Id;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        // GET: Lesson/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.ClassGroup)
                .Include(l => l.Room)
                .Include(l => l.Subject)
                .Include(l => l.Teacher)
                .Include(l => l.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // POST: Lesson/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.Id == id);
        }

        private void FillLists(LessonCreateViewModel vm)
        {
            vm.Teachers = _context.Teachers.Include(t => t.User).Include(t => t.Subjects).ToList();
            vm.Subjects = _context.Subjects.ToList();
            vm.Rooms = _context.Rooms.ToList();
            vm.ClassGroups = _context.ClassGroups.ToList();
            vm.TimeSlots = _context.TimeSlots.ToList();
        }

    }
}
