using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;

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

        // GET: Lesson/Create
        public IActionResult Create()
        {
            ViewData["ClassGroupId"] = new SelectList(_context.ClassGroups, "Id", "Id");
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id");
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Code");
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId");
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "Id", "Id");
            return View();
        }

        // POST: Lesson/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SubjectId,TeacherId,ClassGroupId,RoomId,TimeSlotId")] Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lesson);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassGroupId"] = new SelectList(_context.ClassGroups, "Id", "Id", lesson.ClassGroupId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", lesson.RoomId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Code", lesson.SubjectId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "UserId", "UserId", lesson.TeacherId);
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "Id", "Id", lesson.TimeSlotId);
            return View(lesson);
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
    }
}
