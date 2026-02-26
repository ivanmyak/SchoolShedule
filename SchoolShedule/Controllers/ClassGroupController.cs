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
    public class ClassGroupController : Controller
    {
        private readonly SheduleDbContext _context;

        public ClassGroupController(SheduleDbContext context)
        {
            _context = context;
        }

        // GET: ClassGroup
        public async Task<IActionResult> All()
        {
            ViewData["Title"] = "Классы";

            return View(await _context.ClassGroups.ToListAsync());
        }

        // GET: ClassGroup/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classGroup = await _context.ClassGroups
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classGroup == null)
            {
                return NotFound();
            }

            return View(classGroup);
        }

        // GET: ClassGroup/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ClassGroup/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Letter,Grade")] ClassGroup classGroup)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classGroup);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(All));
            }
            return View(classGroup);
        }

        // GET: ClassGroup/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classGroup = await _context.ClassGroups.FindAsync(id);
            if (classGroup == null)
            {
                return NotFound();
            }
            return View(classGroup);
        }

        // POST: ClassGroup/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Letter,Grade")] ClassGroup classGroup)
        {
            if (id != classGroup.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classGroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassGroupExists(classGroup.Id))
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
            return View(classGroup);
        }

        // GET: ClassGroup/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classGroup = await _context.ClassGroups
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classGroup == null)
            {
                return NotFound();
            }

            return View(classGroup);
        }

        // POST: ClassGroup/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classGroup = await _context.ClassGroups.FindAsync(id);
            if (classGroup != null)
            {
                _context.ClassGroups.Remove(classGroup);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        private bool ClassGroupExists(int id)
        {
            return _context.ClassGroups.Any(e => e.Id == id);
        }
    }
}
