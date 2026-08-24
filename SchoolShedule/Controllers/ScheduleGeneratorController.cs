using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Entities.Generation;
using SchoolShedule.Entities.Generation.Rules;
using SchoolShedule.Models;
using System.Runtime.Serialization;
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
            var templates = await _context.ScheduleTemplates
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync();

            return View(templates);

        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.ScheduleTemplates
                .Include(t => t.ClassGroups)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound();

            _context.ScheduleTemplates.Remove(template);

            var containers = _context.LessonRuleContainers
                .Where(c => c.TemplateId == id);

            _context.LessonRuleContainers.RemoveRange(containers);

            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Страница настройки правил для предметов (пока без реализации).
        /// </summary>
        public IActionResult WizardRules(GeneratorWizardViewModel vm)
        {
            // TODO: позже подгрузить RuleBase-наборы и отобразить их.
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CheckTemplateName([FromBody] TemplateNameCheckDto dto)
        {
            var template = await _context.ScheduleTemplates.FindAsync(dto.TemplateId);

            if (template is null)
            {
                return Ok(new { status = "OK" });
            }
            else
            {
                if (string.Equals(template.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
                    return Ok(new { status = "NAME_EXISTS" });
                else
                    return Ok(new { status = "OK" });
            }


        }


        /// <summary>
        /// Минимальный мастер генерации расписания на основе правила HoursRule.
        /// </summary>
        public async Task<IActionResult> WizardTemplateMinimumAsync(int templateId = 0)
        {
            if (templateId == 0)
            {
                var vmnew = new GeneratorWizardViewModel
                {
                    ClassGroups = await _context.ClassGroups.OrderBy(cg => cg.Grade).ThenBy(cg => cg.Letter).ToListAsync(),
                    Subjects = await _context.Subjects.ToListAsync()
                };

                return View(vmnew);
            }
            var template = await _context.ScheduleTemplates
                .Include(t => t.ClassGroups)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template is null)
            {
                return NotFound("Не найден шаблон.");
            }

            var vm = new GeneratorWizardViewModel
            {
                TemplateId = template.Id,
                TemplateName = template.Name,
                PeriodType = template.PeriodType,
                ClassGroups = await _context.ClassGroups
                    .OrderBy(cg => cg.Grade).ThenBy(cg => cg.Letter)
                    .ToListAsync(),
                Subjects = await _context.Subjects.ToListAsync()
            };

            // подставляем часы из правил
            var containers = await _context.LessonRuleContainers
                .Where(c => c.TemplateId == templateId)
                .ToListAsync();

            vm.PreloadedHours = containers
    .Select(c =>
    {
        var rules = JsonSerializer.Deserialize<List<HoursRule>>(c.RulesJson) ?? throw new SerializationException("Не удалось десериализовать правила из шаблона!");
        var hr = rules.First();

        return new WizardHoursCellDto
        {
            SubjectId = c.SubjectId,
            ClassGroupId = c.ClassGroupId,
            Hours = hr.Hours
        };
    })
    .ToList();

            return View(vm);
        }


        /// <summary>
        /// Сохранение настроек часов (HoursRule) для предметов и классов.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> WizardTemplateMinimumSave([FromBody] WizardTempHoursDto dto)
        {
            // создаём шаблон
            ScheduleTemplate template;
            using var tx = await _context.Database.BeginTransactionAsync();

            if (dto.TemplateId == 0)   // Если новый (Id==0) - создаём
            {
                template = new ScheduleTemplate
                {
                    Name = dto.Name,
                    PeriodType = dto.PeriodType,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ScheduleTemplates.Add(template);
                _context.SaveChanges();
            }
            else  // Если существует шаблон (Id!=0) - достаём сущность, присваиваем новые данные и обновляем
            {
                template = await _context.ScheduleTemplates.Include(st=>st.ClassGroups).FirstOrDefaultAsync(st => st.Id == dto.TemplateId) ?? throw new KeyNotFoundException("[WIZARD-SAVE] Не удалось найти Шаблон по ID");
                
                // Меняем прошлые поля на новые
                template.PeriodType = dto.PeriodType;
                template.Name = dto.Name;
                template.UpdatedAt = DateTime.UtcNow;

                // Удаляем старые контейнеры правил
                var oldContainers = _context.LessonRuleContainers
                    .Where(c => c.TemplateId == template.Id);

                _context.LessonRuleContainers.RemoveRange(oldContainers);
            }

            // Сохраняем изменения шаблона
            await _context.SaveChangesAsync();

            // Создаём новые контейнеры правил
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
                    RulesJson = JsonSerializer.Serialize(new List<HoursRule> { rule })
                };

                _context.LessonRuleContainers.Add(container);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

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
