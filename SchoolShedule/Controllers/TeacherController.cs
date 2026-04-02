using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;
using SchoolShedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolShedule.Controllers
{
    public class TeacherController : Controller
    {
        private readonly SheduleDbContext _context;
        private readonly UserManager<User> _userManager;

        private readonly ILogger<TeacherController> _logger;
        private readonly ICredentialsGenerator _generator;

        public TeacherController(SheduleDbContext context, UserManager<User> userManager, ILogger<TeacherController> logger, ICredentialsGenerator generator)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _generator = generator;
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
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"{teacher.GetFIO} - инфо";
            return View(teacher);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAsync()
        {
            ViewData["Title"] = "Новый Преподаватель";

            var model = new TeacherCreateViewModel
            {
                Subjects = await _context.Subjects.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(TeacherCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            // 1. Генерация логина/почты/пароля
            string userName;
            string email;
            string password;

            if (model.AutoGenerateCredentials)
            {
                userName = await _generator.GenerateUserNameAsync(model.FirstName, model.LastName);
                email = await _generator.GenerateEmailAsync(userName);
                password = _generator.GeneratePassword(model.FirstName, model.LastName);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError("", "Email и пароль обязательны, если отключена автогенерация");
                    model.Subjects = await _context.Subjects.ToListAsync();
                    return View(model);
                }

                userName = model.Email;
                email = model.Email;
                password = model.Password;
            }

            // 2. Создаём пользователя
            var user = new User
            {
                UserName = userName,
                Email = email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                RegisteredAt = DateTimeOffset.UtcNow
            };

            var userResult = await _userManager.CreateAsync(user, password);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            // 3. Создаём учителя
            var teacher = new Teacher
            {
                UserId = user.Id,
                Subjects = await _context.Subjects
                    .Where(s => model.SelectedSubjectIds.Contains(s.Id))
                    .ToListAsync()
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(t => t.UserId == id);

            if (teacher == null)
                return NotFound();

            var model = new TeacherUpdateViewModel
            {
                Id = teacher.UserId,
                FirstName = teacher.User.FirstName,
                LastName = teacher.User.LastName,
                MiddleName = teacher.User.MiddleName,
                Email = teacher.User.Email!,
                SelectedSubjectIds = teacher.Subjects.Select(s => s.Id).ToList(),
                Subjects = await _context.Subjects.ToListAsync()
            };

            ViewData["Title"] = $"{teacher.GetFIO}-Изменение";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(t => t.UserId == model.Id);

            if (teacher == null)
                return NotFound();

            // 1. Обновляем User
            teacher.User.FirstName = model.FirstName;
            teacher.User.LastName = model.LastName;
            teacher.User.MiddleName = model.MiddleName;
            teacher.User.Email = model.Email;
            teacher.User.UserName = model.Email;

            await _userManager.UpdateAsync(teacher.User);

            // 2. Обновляем предметы
            teacher.Subjects = await _context.Subjects
                .Where(s => model.SelectedSubjectIds.Contains(s.Id))
                .ToListAsync();

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
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
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"! {teacher.GetFIO}-УДАЛЕНИЕ !";

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
    }
}
