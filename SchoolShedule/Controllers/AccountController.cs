using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;
using SchoolShedule.Models;

namespace SchoolShedule.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly SheduleDbContext _context;

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(SheduleDbContext context, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;

            _logger = logger;
        }


        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неверный Email или пароль");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Administrator")]
        [Route("Account/initdb")]
        public async Task<IActionResult> InitSeedDb([FromServices] RoleManager<IdentityRole<Guid>> roleManager, [FromServices] ICredentialsGenerator generator)
        {
            await Data.Seed.DbInitializer.SeedAsync(_context, _logger,_userManager, roleManager, generator);
            return Ok(new { message = "Инициализация выполнена" });
        }

        [Route("StartShedule")]
        public async Task<IActionResult> InitAdminDb([FromServices] RoleManager<IdentityRole<Guid>> roleManager)
        {
            await Data.Seed.DbInitializer.InitAdminAsync(_userManager, roleManager, _logger);
            return Ok(new { message = "Инициализация выполнена" });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ResetPasswordByAdmin(Guid id, [FromServices] ICredentialsGenerator generator, [FromServices] RoleManager<IdentityRole<Guid>> roleManager)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User).FirstOrDefaultAsync(m => m.UserId == id);
            if (teacher == null)
            {
                _logger.LogWarning($"[ACCOUNT-RESETPASSWORD] Не удалось сбросить пароль учителю - не удалось найти!");

                return NotFound();
            }

            // Сброс пароля на новый
            string newPassword = generator.GeneratePassword(teacher.User.FirstName, teacher.User.LastName);
            var token = await _userManager.GeneratePasswordResetTokenAsync(teacher.User);
            var result = await _userManager.ResetPasswordAsync(teacher.User, token, newPassword);

            //Проверка соответствующей роли у учителя и её присвоение
            if (!await _userManager.IsInRoleAsync(teacher.User, "Teacher"))
            {
                _logger.LogWarning($"[ACCOUNT-RESETPASSWORD] У Учителя {teacher.GetFIO} отсутствует привязка к роли - исправляем!");
                await _userManager.AddToRoleAsync(teacher.User, "Teacher");
            }
            if (result.Succeeded)
            {
                _logger.LogInformation($"Пароль учителя {teacher.GetFIO} успешно сброшен Администратором.");
                return RedirectToAction("All", "Teacher");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return RedirectToAction("All", "Teacher");
        }
    }
}
