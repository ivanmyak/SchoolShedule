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
    public class TeacherController : Controller
    {
        private readonly SheduleDbContext _context;

        public TeacherController(SheduleDbContext context)
        {
            _context = context;
        }

        // GET: Teacher
        public async Task<IActionResult> All()
        {
            var sheduleDbContext = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects);

            ViewData["Title"] = "Преподаватели";

            return View(await sheduleDbContext.ToListAsync());
        }

        // GET: Teacher/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // GET: Teacher/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Новый Преподаватель";

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FirstName");
            return View();
        }

        // POST: Teacher/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId")] Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                teacher.UserId = Guid.NewGuid();
                _context.Add(teacher);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(All));
            }
            ViewData["Title"] = "Новый Преподаватель";
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FirstName", teacher.UserId);
            return View(teacher);
        }

        // GET: Teacher/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FirstName", teacher.UserId);
            return View(teacher);
        }

        // POST: Teacher/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("UserId")] Teacher teacher)
        {
            if (id != teacher.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(All));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FirstName", teacher.UserId);
            return View(teacher);
        }

        // GET: Teacher/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teacher/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        private bool TeacherExists(Guid id)
        {
            return _context.Teachers.Any(e => e.UserId == id);
        }
    }
}
