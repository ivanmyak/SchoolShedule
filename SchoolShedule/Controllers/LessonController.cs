using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Enums;
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

        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }
            ViewData["ClassGroupId"] = new SelectList(_context.ClassGroups, "Id", "Id", lesson.ClassGroupId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", lesson.RoomId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Code", lesson.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId", lesson.TeacherId);
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "Id", "Id", lesson.TimeSlotId);
            return View(lesson);
        }

        // POST: Lesson/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SubjectId,TeacherId,ClassGroupId,RoomId,TimeSlotId")] Lesson lesson)
        {
            if (id != lesson.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lesson);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LessonExists(lesson.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassGroupId"] = new SelectList(_context.ClassGroups, "Id", "Id", lesson.ClassGroupId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", lesson.RoomId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Code", lesson.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId", lesson.TeacherId);
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "Id", "Id", lesson.TimeSlotId);
            return View(lesson);
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
