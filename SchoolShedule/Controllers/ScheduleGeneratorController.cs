using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities.Generation;
using SchoolShedule.Models;

namespace SchoolShedule.Controllers
{
    /// <summary>
    /// Контроллер мастера генерации расписания.
    /// </summary>
    public class ScheduleGeneratorController : Controller
    {
        private readonly SheduleDbContext _context;

        /// <summary>
        /// Создаёт новый экземпляр контроллера мастера генерации расписания.
        /// </summary>
        public ScheduleGeneratorController(SheduleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Информационная страница по текущим расписаниям и шаблонам.
        /// </summary>
        public IActionResult Index()
        {
            // TODO: позже добавить информацию о существующих шаблонах и расписаниях.
            return View();
        }

        /// <summary>
        /// Старт мастера генерации расписания: выбор периода и классов.
        /// </summary>
        public async Task<IActionResult> Wizard()
        {
            var vm = new GeneratorWizardViewModel();
            vm.Teachers = await _context.Teachers.Include("User").Include("Subjects").ToListAsync();
            vm.Subjects = await _context.Subjects.ToListAsync();
            vm.ClassGroups = await _context.ClassGroups.OrderBy(cg => cg.Grade).ThenBy(cg => cg.Letter).ToListAsync();
            return View("WizardStep1", vm);
        }

        /// <summary>
        /// Обработка выбора периода и классов, создание черновика шаблона.
        /// </summary>
        [HttpPost]
        public IActionResult WizardStep1(GeneratorWizardViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Wizard", vm);

            var template = new ScheduleTemplate
            {
                PeriodType = vm.PeriodType!.Value
            };

            // Загружаем классы из БД
            var classGroups = _context.ClassGroups
                .Where(c => vm.SelectedClassIds.Contains(c.Id))
                .ToList();

            template.ClassGroups.AddRange(classGroups);

            _context.ScheduleTemplates.Add(template);
            _context.SaveChanges();

            vm.TemplateId = template.Id;

            return RedirectToAction("WizardStep2", vm);
        }

        /// <summary>
        /// Шаг выбора предметов для генерации расписания.
        /// </summary>
        public IActionResult WizardStep2(GeneratorWizardViewModel vm)
        {
            // TODO: подгрузить список предметов для выбранных классов.
            return View(vm);
        }

        /// <summary>
        /// Обработка выбора предметов и сохранение их в шаблон.
        /// </summary>
        [HttpPost]
        public IActionResult WizardStep2Post(GeneratorWizardViewModel vm)
        {
            if (vm.TemplateId is null)
                return RedirectToAction("Wizard");

            var template = _context.ScheduleTemplates.Find(vm.TemplateId.Value);
            if (template is null)
                return RedirectToAction("Wizard");

            // пока просто сохраняем выбранные предметы в шаблон (можно добавить отдельное поле при необходимости)
            // либо позже вынести в отдельную сущность.

            _context.SaveChanges();

            return RedirectToAction("WizardStep3", vm);
        }

        /// <summary>
        /// Шаг выбора преподавателей для выбранных предметов.
        /// </summary>
        public IActionResult WizardStep3(GeneratorWizardViewModel vm)
        {
            // TODO: подгрузить преподавателей по предметам.
            return View(vm);
        }

        /// <summary>
        /// Обработка выбора преподавателей и переход к настройке правил.
        /// </summary>
        [HttpPost]
        public IActionResult WizardStep3Post(GeneratorWizardViewModel vm)
        {
            if (vm.TemplateId is null)
                return RedirectToAction("Wizard");

            // TODO: сохранить выбранных преподавателей в связанной сущности/шаблоне.

            return RedirectToAction("WizardRules", vm);
        }

        /// <summary>
        /// Страница настройки правил для предметов (пока без реализации).
        /// </summary>
        public IActionResult WizardRules(GeneratorWizardViewModel vm)
        {
            // TODO: позже подгрузить RuleBase-наборы и отобразить их.
            return View(vm);
        }
    }
}
