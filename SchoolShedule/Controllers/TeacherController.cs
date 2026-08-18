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
using System.Data;
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

        public async Task<IActionResult> All()
        {
            _logger.LogInformation("Получение списка преподавателей");

            var sheduleDbContext = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects);

            ViewData["Title"] = "Преподаватели";

            return View(await sheduleDbContext.ToListAsync());
        }

        // GET: Teacher/Details/5
        public async Task<IActionResult> DetailsAsync(Guid? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details: передан пустой Id");
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                _logger.LogWarning("Details: преподаватель не найден, Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Просмотр преподавателя: {FIO}", teacher.User.GetFIO);
            ViewData["Title"] = $"{teacher.User.GetFIO} - инфо";
            return View(teacher);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAsync()
        {
            ViewData["Title"] = "Новый Преподаватель";
            _logger.LogInformation("Открытие формы создания преподавателя");

            var model = new TeacherCreateViewModel
            {
                Subjects = await _context.Subjects.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(TeacherCreateViewModel model, [FromServices] RoleManager<IdentityRole<Guid>> roleManager)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Создание преподавателя: модель невалидна");
                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            // 1. Генерация логина/почты/пароля
            string userName;
            string email;
            string password;

            if (model.AutoGenerateCredentials)
            {
                _logger.LogInformation("Генерация учётных данных для преподавателя {LastName} {FirstName}",
                model.LastName, model.FirstName);

                userName = await _generator.GenerateUserNameAsync(model.FirstName, model.LastName);
                email = await _generator.GenerateEmailAsync(userName);
                password = _generator.GeneratePassword(model.FirstName, model.LastName);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    _logger.LogWarning("Создание преподавателя: Email/Password не указаны при отключённой автогенерации");
                    ModelState.AddModelError("", "Email и пароль обязательны, если отключена автогенерация");
                    model.Subjects = await _context.Subjects.ToListAsync();
                    return View(model);
                }

                userName = model.Email;
                email = model.Email;
                password = model.Password;
            }

            //2. Проверяем наличие роли Учитель
            var teacherRole = await roleManager.FindByNameAsync("Teacher");
            if (teacherRole == null)
            {
                _logger.LogWarning("[TEACHER-CREATE] В БД отсутствует роль Учителя! Выполняем создание даной роли!");
                teacherRole = new() { Name = "Teacher" };
                var resultRole = await roleManager.CreateAsync(teacherRole);
                if (resultRole.Succeeded) // И, после создания Юзверя и Роли - объединяем их (PanPineappleApplePan!)
                {
                    _logger.LogWarning("[TEACHER-CREATE] В БД успешно создана роль Учителя!");
                }
                else
                {
                    _logger.LogError("[TEACHER-CREATE] Не удалось создать роль Учителя в БД!");
                    throw new DbUpdateException("Не удалось создать роль Учителя в БД!");
                }
            }

            // 3. Создаём пользователя
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
                _logger.LogError("[TEACHER-CREATE] Ошибка создания пользователя: {Errors}",
                string.Join(", ", userResult.Errors.Select(e => e.Description)));

                foreach (var error in userResult.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            //3.1. Присваиваем роль - Учителю
            await _userManager.AddToRoleAsync(user, teacherRole.Name ?? "Teacher");

            // 4. Создаём учителя
            var teacher = new Teacher
            {
                UserId = user.Id,
                Subjects = await _context.Subjects
                    .Where(s => model.SelectedSubjectIds.Contains(s.Id))
                    .ToListAsync()
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[TEACHER-CREATE] Создан преподаватель: {FIO}, Email={Email}", teacher.User.GetFIO, email);

            return RedirectToAction(nameof(All));
        }


        [HttpGet]
        public async Task<IActionResult> EditAsync(Guid id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(t => t.UserId == id);

            if (teacher == null)
            {
                _logger.LogWarning("[TEACHER-EDIT] Преподаватель не найден, Id={Id}", id);
                return NotFound();
            }
            _logger.LogInformation("[TEACHER-EDIT] Открытие формы редактирования преподавателя: {FIO}", teacher.User.GetFIO);

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

            ViewData["Title"] = $"{teacher.User.GetFIO}-Изменение";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(TeacherUpdateViewModel model, [FromServices] RoleManager<IdentityRole<Guid>> roleManager)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[TEACHER-EDIT] Модель невалидна");

                model.Subjects = await _context.Subjects.ToListAsync();
                return View(model);
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(t => t.UserId == model.Id);

            if (teacher == null)
            {
                _logger.LogWarning("[TEACHER-EDIT] Преподаватель не найден, Id={Id}", model.Id);
                return NotFound();
            }

            //2. Проверяем наличие роли Учитель
            var teacherRole = await roleManager.FindByNameAsync("Teacher");
            if (teacherRole == null)
            {
                _logger.LogWarning("[TEACHER-EDIT] В БД отсутствует роль Учителя! Выполняем создание даной роли!");
                teacherRole = new() { Name = "Teacher" };
                var resultRole = await roleManager.CreateAsync(teacherRole);
                if (resultRole.Succeeded) // И, после создания Юзверя и Роли - объединяем их (PanPineappleApplePan!)
                {
                    _logger.LogWarning("[TEACHER-EDIT] В БД успешно создана роль Учителя!");
                }
                else
                {
                    _logger.LogError("[TEACHER-EDIT] Не удалось создать роль Учителя в БД!");
                    throw new DbUpdateException("Не удалось создать роль Учителя в БД!");
                }
            }

            //Проверка соответствующей роли у учителя и её присвоение
            if (!await _userManager.IsInRoleAsync(teacher.User, "Teacher"))
            {
                _logger.LogWarning($"[TEACHER-EDIT] У Учителя {teacher.User.GetFIO} отсутствует привязка к роли - исправляем!");
                await _userManager.AddToRoleAsync(teacher.User, "Teacher");
            }

            _logger.LogInformation("[TEACHER-EDIT] Редактирование преподавателя: {FIO}", teacher.User.GetFIO);
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

            _logger.LogInformation("Преподаватель обновлён: {FIO}", teacher.User.GetFIO);

            return RedirectToAction(nameof(All));
        }

        // GET: Teacher/Delete/5
        public async Task<IActionResult> DeleteAsync(Guid? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete: передан пустой Id");
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Subjects)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                _logger.LogWarning("Delete: преподаватель не найден, Id={Id}", id);
                return NotFound();
            }
            ViewData["Title"] = $"{teacher.User.GetFIO}-УДАЛЕНИЕ";

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
                _logger.LogInformation("Удаление преподавателя: UserId={Id}, FIO={Fio}, Subject={Subjects}", id, teacher.User.GetFIO, teacher.SubjectsToString);
                _context.Teachers.Remove(teacher);
            }
            else
            {
                _logger.LogWarning("Попытка удалить несуществующего преподавателя: Id={Id}", id);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }
    }
}
