using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolShedule.Controllers
{
    public class RoomController : Controller
    {
        private readonly ILogger<RoomController> _logger;
        private readonly SheduleDbContext _context;

        public RoomController(SheduleDbContext context, ILogger<RoomController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> All()
        {
            ViewData["Title"] = "Кабинеты";

            return View(await _context.Rooms
                .Include(r => r.AssignedSubjects)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            ViewData["Title"] = "Details Room";

            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        public async Task<IActionResult> CreateAsync()
        {
            ViewData["Title"] = "Create Room";

            var model = new RoomCreateViewModel
            {
                Subjects = await _context.Subjects.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RoomCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.Rooms
                     .Any(r => r.Floor == model.Floor && r.Number == model.Number);
                if (exists)
                    throw new InvalidOperationException($"[ROOM-CREATE] Кабинет {model.Floor}-{model.Number} уже существует.");

                Room room = new()
                {
                    Floor = model.Floor,
                    Number = model.Number,
                    AssignedSubjects = await _context.Subjects
                    .Where(s => model.SelectedSubjectIds.Contains(s.Id))
                    .ToListAsync()
                };

                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(All));
            }
            return View(model);
        }

        public async Task<IActionResult> EditAsync(int? id)
        {
            ViewData["Title"] = "Edit Room";

            if (id == null)
            {
                return NotFound("[ROOM-EDIT] Не указан ID-кабинета для изменения!");
            }

            var room = await _context.Rooms.Include(r => r.AssignedSubjects).SingleOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                _logger.LogError($"[ROOM-EDIT] Не найден кабинет по ID=({id})!");
                return NotFound($"[ROOM-EDIT] Не найден выбранный кабинет!");
            }
            RoomEditViewModel model = new()
            {
                Id = room.Id,
                Floor = room.Floor,
                Number = room.Number,
                SelectedSubjectIds = room.AssignedSubjects.Select(s => s.Id).ToList(),
                Subjects = await _context.Subjects.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RoomEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[ROOM-EDIT] Модель невалидна");

                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            var room = await _context.Rooms
                .Include(r => r.AssignedSubjects)
               .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (room == null)
            {
                _logger.LogWarning("[ROOM-EDIT] Кабинет не найден, Id={Id}", model.Id);
                return NotFound("Кабинет не найден");
            }
            _logger.LogInformation("[ROOM-EDIT] Редактирование кабинета: {RoomNumber}", room.RoomNumber);

            // Обновляем предметы
            room.AssignedSubjects = await _context.Subjects
                .Where(s => model.SelectedSubjectIds.Contains(s.Id))
                .ToListAsync();

            _context.Update(room);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[ROOM-EDIT] Кабинет обновлён: {RoomNumber}", room.RoomNumber);
            return RedirectToAction(nameof(All));
        }

        public async Task<IActionResult> DeleteAsync(int? id)
        {
            ViewData["Title"] = "Delete Room";

            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}
