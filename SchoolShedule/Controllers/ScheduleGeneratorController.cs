using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Entities.Generation;
using SchoolShedule.Entities.Generation.Rules;
using SchoolShedule.Models;
using System.Text.Json;

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
            // Тут мы, когда-нибудь, приделаем передачу выбранных шаблонов или расписаний для корректировок/перегенерации
            return View();
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

        /// <summary>
        /// Минимальный мастер генерации расписания на основе правила HoursRule.
        /// </summary>
        public async Task<IActionResult> WizardTemplateMinimumAsync()
        {
            var vm = new GeneratorWizardViewModel
            {
                ClassGroups = await _context.ClassGroups.OrderBy(cg => cg.Grade).ThenBy(cg => cg.Letter).ToListAsync(),
                Subjects = await _context.Subjects.ToListAsync()
            };

            return View(vm);
        }

        /// <summary>
        /// Сохранение настроек часов (HoursRule) для предметов и классов.
        /// </summary>
        [HttpPost]
        public IActionResult WizardTemplateMinimumSave([FromBody] WizardHoursDto dto)
        {
            // создаём шаблон
            var template = new ScheduleTemplate
            {
                PeriodType = dto.PeriodType
            };

            _context.ScheduleTemplates.Add(template);
            _context.SaveChanges();

            // для каждой ячейки таблицы создаём HoursRule
            foreach (var cell in dto.Cells)
            {
                if (cell.Hours <= 0)
                    continue;

                var rule = new HoursRule
                {
                    SubjectId = cell.SubjectId,
                    ClassGroupId = cell.ClassGroupId,
                    Hours = cell.Hours,
                    PeriodType = dto.PeriodType
                };

                var container = new LessonRuleContainer
                {
                    TemplateId = template.Id,
                    SubjectId = cell.SubjectId,
                    ClassGroupId = cell.ClassGroupId,
                    RulesJson = JsonSerializer.Serialize(new List<RuleBase> { rule })
                };

                _context.LessonRuleContainers.Add(container);
            }

            _context.SaveChanges();

            return Ok(new { templateId = template.Id });
        }

        /// <summary>
        /// Запуск минимальной генерации расписания на основе сохранённых HoursRule.
        /// </summary>
        [HttpPost]
        public IActionResult WizardTemplateMinimumGenerate([FromBody] int templateId)
        {
            var template = _context.ScheduleTemplates
                .Include(t => t.ClassGroups)
                .FirstOrDefault(t => t.Id == templateId);

            var containers = _context.LessonRuleContainers
                .Where(c => c.TemplateId == templateId)
                .ToList();

            // TODO: здесь будет минимальный генератор:
            // - по каждому контейнеру
            // - по каждому HoursRule
            // - создаём Lesson в нужном количестве

            return Ok();
        }
    }
}
