using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolShedule.Controllers
{
    public class SubjectController : Controller
    {
        private readonly ILogger<SubjectController> _logger;
        private readonly SheduleDbContext _context;

        public SubjectController(SheduleDbContext context, ILogger<SubjectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Subject
        public async Task<IActionResult> All()
        {
            ViewData["Title"] = "Предметы";
            _logger.LogInformation("Получение списка предметов");
            return View(await _context.Subjects.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> DetailsAsync(int? id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                _logger.LogWarning("Попытка открыть Details для несуществующего предмета: Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Просмотр предмета: {Name} ({Code})", subject.Name, subject.Code);
            ViewData["Title"] = $"{subject.Name} - инфо";

            return View(subject);
        }

        [HttpGet]
        public IActionResult CreateAsync()
        {
            _logger.LogInformation("Открытие формы создания предмета");
            ViewData["Title"] = $"Предмет - новый";

            return View(new Subject());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(Subject subject)
        {
            subject.Code = subject.Code?.Trim().ToUpper() ?? throw new Exception("При создании нового Предмета поле Code оказалось пустым!");

            // Проверка уникальности Code
            var duplicates = await _context.Subjects
                .Where(s => s.Code == subject.Code)
                .ToListAsync();

            if (duplicates.Any())
            {
                _logger.LogWarning("Попытка создать предмет с дублирующим кодом: {Code}", subject.Code);
                ModelState.AddModelError("Code", "Код предмета должен быть уникальным");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Создание предмета не удалось из-за ошибок валидации");
                return View(subject);
            }

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создан новый предмет: {Name} ({Code})", subject.Name, subject.Code);

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> EditAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                _logger.LogWarning("Попытка открыть Edit для несуществующего предмета: Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Открытие формы редактирования предмета: {Name} ({Code})", subject.Name, subject.Code);
            ViewData["Title"] = $"{subject.Name}-Изменение";

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(int id, Subject model)
        {
            if (id != model.Id)
            {
                _logger.LogError("Несоответствие Id при редактировании: routeId={RouteId}, modelId={ModelId}", id, model.Id);
                return NotFound();
            }
            var subject = await _context.Subjects.FindAsync(model.Id);

            if (subject == null)
            {
                _logger.LogWarning("Редактирование: предмет ({Name} - {Code}) не найден, Id={Id}", model.Name, model.Code, model.Id);
                return NotFound();
            }


            var duplicates = await _context.Subjects
                .Where(s => s.Id != model.Id && s.Code == model.Code)
                .ToListAsync();

            if (duplicates.Any())
            {
                _logger.LogWarning("Попытка изменить предмет на дублирующий код: {Code}", subject.Code);
                ModelState.AddModelError("Code", "Код предмета должен быть уникальным");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Редактирование предмета не удалось из-за ошибок валидации");
                return View(subject);
            }

            string normalisedCode = model.Code?.Trim().ToUpper() ?? throw new Exception("При создании нового Предмета поле Code оказалось пустым!");
            if (subject.Name != model.Name)
                subject.Name = model.Name;

            if (subject.Code != normalisedCode)
                subject.Code = normalisedCode;
            _context.Update(subject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Предмет обновлён: {Name} ({Code})", subject.Name, subject.Code);

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                _logger.LogWarning("Попытка открыть Delete для несуществующего предмета: Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Открытие формы удаления предмета: {Name} ({Code})", subject.Name, subject.Code);
            ViewData["Title"] = $"{subject.Name}-УДАЛЕНИЕ";

            return View(subject);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject != null)
            {
                _logger.LogInformation("Удаление предмета: {Name} ({Code})", subject.Name, subject.Code);
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Попытка удалить несуществующий предмет: Id={Id}", id);
            }

            return RedirectToAction(nameof(All));
        }
    }
}
