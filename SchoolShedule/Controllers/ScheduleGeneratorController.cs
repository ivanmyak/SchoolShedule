using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Entities.Generation;
using SchoolShedule.Entities.Generation.Rules;
using SchoolShedule.Enums;
using SchoolShedule.Extensions;
using SchoolShedule.Models;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace SchoolShedule.Controllers
{
    /// <summary>
    /// Контроллер мастера генерации расписания.
    /// </summary>
    public class ScheduleGeneratorController : Controller
    {
        /// <summary>
        /// Максимум уроков в день.
        /// </summary>
        /// <remarks>Можно высчитывать из имеющихся в БД TimeSlot, но пока закостылил</remarks>
        private static readonly int _maxCountLessonsPerDay = 6;
        /// <summary>
        /// Все "ячейки" для уроков, возможные на неделе
        /// </summary>
        private readonly int _maxCountLessonsPerWeek = _maxCountLessonsPerDay * 5;


        private readonly ILogger<ScheduleGeneratorController> _logger;
        private readonly SheduleDbContext _context;

        /// <summary>
        /// Создаёт новый экземпляр контроллера мастера генерации расписания.
        /// </summary>
        public ScheduleGeneratorController(SheduleDbContext context, ILogger<ScheduleGeneratorController> logger)
        {
            _context = context;
            _logger = logger;
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
        /// Обработка загружаемого шаблона
        /// </summary>
        /// <param name="file">Шаблон в виде Excel-файла</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadTemplateExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не загружен.");

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            // Загружаем справочники
            var allClassesDict = _context.ClassGroups
                .ToDictionary(c => c.Name.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

            var allSubjectsDict = _context.Subjects
                .ToDictionary(s => s.Name.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

            // Шаблоны — ключ: (Name + PeriodType)
            var allTemplatesDict = _context.ScheduleTemplates
                .ToDictionary(
                    t => $"{t.Name.Trim().ToLower()}|{t.PeriodType}",
                    t => t
                );

            // Валидация Excel
            var report = ValidateExcel(ws, allClassesDict, allSubjectsDict, allTemplatesDict);

            if (report.HasErrors)
            {
                TempData["ExcelErrors"] = string.Join("\n", report.Errors);
                return RedirectToAction("Wizard");
            }

            if (report.HasWarnings)
            {
                TempData["ExcelWarnings"] = string.Join("\n", report.Warnings);
                // Можно сделать страницу подтверждения
            }

            // 1. Читаем период
            var periodType = Enum.Parse<SchedulePeriodType>(ws.Cells["B1"].Text);

            // 2. Читаем название шаблона
            var templateName = ws.Cells["B2"].Text.Trim();

            // 3. Проверка существующего шаблона
            string templateKey = $"{templateName.ToLower()}|{periodType}";

            if (allTemplatesDict.ContainsKey(templateKey))
            {
                TempData["ExcelErrors"] =
                    $"Уже существует шаблон с именем '{templateName}' и периодом '{periodType}'.\n" +
                    $"Перед загрузкой такого же шаблона — либо переименуйте Excel-файл, либо удалите существующий.\n" +
                    $"Настоятельно рекомендуем перед удалением — выгрузить шаблон.";
                return RedirectToAction("Wizard");
            }

            // 4. Создаём шаблон
            var template = new ScheduleTemplate
            {
                Name = templateName,
                PeriodType = periodType,
                CreatedAt = DateTime.UtcNow
            };

            _context.ScheduleTemplates.Add(template);
            await _context.SaveChangesAsync();

            // 5. Читаем таблицу предмет × класс
            int row = 5;

            while (!string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text))
            {
                string subjectName = ws.Cells[row, 1].Text.Trim();
                var subject = allSubjectsDict[subjectName];

                int col = 2;

                while (!string.IsNullOrWhiteSpace(ws.Cells[4, col].Text))
                {
                    string className = ws.Cells[4, col].Text.Trim();
                    var classGroup = allClassesDict[className];

                    int hours = int.Parse(ws.Cells[row, col].Text);

                    var rule = new HoursRule
                    {
                        Hours = hours,
                        PeriodType = periodType,
                        SubjectId = subject.Id
                    };

                    var container = new LessonRuleContainer
                    {
                        TemplateId = template.Id,
                        SubjectId = subject.Id,
                        ClassGroupId = classGroup.Id,
                        RulesJson = JsonSerializer.Serialize(new List<HoursRule> { rule })
                    };

                    _context.LessonRuleContainers.Add(container);

                    col++;
                }

                row++;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Wizard");
        }


        /// <summary>
        /// Экспорт в Excel-файл выбранный шаблон
        /// </summary>
        /// <param name="templateId">ID выбранного шаблона</param>
        /// <returns></returns>
        public async Task<IActionResult> DownloadTemplateExcel(int templateId)
        {
            if (templateId == 0)
                return NotFound("Не выбран шаблон");

            var containers = await _context.LessonRuleContainers
                .Where(c => c.TemplateId == templateId)
                .Include(c => c.ClassGroup)
                .Include(c => c.Subject)
                .Include(c => c.Template)
                .ToListAsync();

            if (!containers.Any())
                return NotFound("У шаблона нет данных.");

            var template = containers.First().Template;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Template");

            // Заголовки
            ws.Cells["A1"].Value = "Период:";
            ws.Cells["B1"].Value = template.PeriodType;

            ws.Cells["A2"].Value = "Название:";
            ws.Cells["B2"].Value = template.Name;

            // -----------------------------
            // 1. Собираем классы из контейнеров
            // -----------------------------
            var classGroups = containers
                .Select(c => c.ClassGroup)
                .Distinct()
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Letter)
                .ToList();

            // Заголовок таблицы
            ws.Cells[4, 1].Value = "Предмет";

            int col = 2;
            foreach (var cg in classGroups)
            {
                ws.Cells[4, col].Value = cg.Name;
                col++;
            }

            // -----------------------------
            // 2. Собираем предметы из контейнеров
            // -----------------------------
            var subjects = containers
                .Select(c => c.Subject)
                .Distinct()
                .OrderBy(s => s.Name)
                .ToList();

            // -----------------------------
            // 3. Заполняем таблицу предмет × класс
            // -----------------------------
            int row = 5;

            foreach (var subject in subjects)
            {
                ws.Cells[row, 1].Value = subject.Name;

                col = 2;

                foreach (var cg in classGroups)
                {
                    var cont = containers
                        .FirstOrDefault(c => c.SubjectId == subject.Id &&
                                             c.ClassGroupId == cg.Id);

                    int hours = 0;

                    if (cont != null)
                    {
                        var rules = JsonSerializer.Deserialize<List<HoursRule>>(cont.RulesJson);
                        hours = rules?.FirstOrDefault()?.Hours ?? 0;
                    }

                    ws.Cells[row, col].Value = hours;
                    col++;
                }

                row++;
            }

            var bytes = package.GetAsByteArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{template.Name}.xlsx"
            );
        }

        private ExcelImportReport ValidateExcel(
    ExcelWorksheet ws,
    Dictionary<string, ClassGroup> allClassesDict,
    Dictionary<string, Subject> allSubjectsDict,
    Dictionary<string, ScheduleTemplate> allTemplatesDict)
        {
            var report = new ExcelImportReport();

            // Проверка структуры
            if (ws.Cells["A1"].Text != "Период:")
                report.Errors.Add("Ячейка A1 должна содержать текст 'Период:'.");

            if (string.IsNullOrWhiteSpace(ws.Cells["B1"].Text))
                report.Errors.Add("Не указан период в ячейке B1.");

            if (!Enum.TryParse<SchedulePeriodType>(ws.Cells["B1"].Text, out var period))
                report.Errors.Add($"Период '{ws.Cells["B1"].Text}' не соответствует типам SchedulePeriodType.");

            if (string.IsNullOrWhiteSpace(ws.Cells["B2"].Text))
                report.Errors.Add("Название шаблона (ячейка B2) пустое.");

            string templateName = ws.Cells["B2"].Text.Trim();
            string templateKey = $"{templateName.ToLower()}|{period}";

            if (allTemplatesDict.ContainsKey(templateKey))
            {
                report.Errors.Add(
                    $"Уже существует шаблон '{templateName}' для периода '{period}'. " +
                    $"Переименуйте Excel-файл или удалите существующий шаблон."
                );
            }

            // Чтение классов
            var classNames = new List<string>();
            int col = 2;

            while (!string.IsNullOrWhiteSpace(ws.Cells[4, col].Text))
            {
                classNames.Add(ws.Cells[4, col].Text.Trim());
                col++;
            }

            if (!classNames.Any())
                report.Errors.Add("Не найдено ни одного класса в строке 4.");

            foreach (var name in classNames)
            {
                if (!allClassesDict.ContainsKey(name))
                    report.Errors.Add($"Класс '{name}' отсутствует в базе данных.");
            }

            // Чтение предметов
            int row = 5;

            while (!string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text))
            {
                string subjectName = ws.Cells[row, 1].Text.Trim();

                if (!allSubjectsDict.ContainsKey(subjectName))
                    report.Errors.Add($"Предмет '{subjectName}' отсутствует в базе данных.");

                col = 2;

                foreach (var className in classNames)
                {
                    string raw = ws.Cells[row, col].Text.Trim();

                    if (string.IsNullOrWhiteSpace(raw))
                        report.Errors.Add($"Пустая ячейка часов для '{subjectName}' и класса '{className}'.");
                    else if (!int.TryParse(raw, out int hours))
                        report.Errors.Add($"Некорректное значение часов '{raw}' для '{subjectName}' и класса '{className}'.");
                    else
                    {
                        if (hours < 0)
                            report.Errors.Add($"Отрицательное количество часов ({hours}) для '{subjectName}' и класса '{className}'.");

                        if (hours > 20)
                            report.Warnings.Add($"Подозрительно большое количество часов ({hours}) для '{subjectName}' и класса '{className}'.");
                    }

                    col++;
                }

                row++;
            }

            return report;
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
                template = await _context.ScheduleTemplates.Include(st => st.ClassGroups).FirstOrDefaultAsync(st => st.Id == dto.TemplateId) ?? throw new KeyNotFoundException("[WIZARD-SAVE] Не удалось найти Шаблон по ID");

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
                    RuleType = rule.GetType().Name, // Для будущей де-сериализации. Пока - костыль HoursRule только
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
        /// Расчёт для "Ручного" формирования расписания 
        /// </summary>
        /// <param name="templateId">ID шаблона, по которому формируются правила расписания</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="SerializationException"></exception>
        public IActionResult BuildManualSchedule(int templateId)
        {
            var classGroupContainers = _context.LessonRuleContainers
    .Where(c => c.TemplateId == templateId)
    .Include(c => c.ClassGroup)
    .Include(c => c.Subject)
    .Include(c => c.Template)
    .OrderBy(c => c.ClassGroup.Grade)
        .ThenBy(c => c.ClassGroup.Letter)
    .GroupBy(ar => ar.ClassGroupId)
    .ToList();

            var allRooms = _context.Rooms.Include(r => r.AssignedSubjects).ToList();
            var allTeachers = _context.Teachers.Include(t => t.User).Include(t => t.Subjects).ToList();
            var allSubjects = _context.Subjects.ToList();
            var allClassGroups = _context.ClassGroups.ToList();

            if (!classGroupContainers.Any())
                throw new KeyNotFoundException($"[WIZARD-GENERATE-MINIMUM] Не найдены правила для выбранного шаблона (ID = {templateId}) в БД");

            // Проверяем, что у каждого предмета есть хотя бы один кабинет
            foreach (var subject in allSubjects)
            {
                bool hasRoom = allRooms.Any(r => r.AssignedSubjects.Any(s => s.Id == subject.Id));
                if (!hasRoom)
                {
                    string msg = $"[WIZARD-GENERATE-MINIMUM] Для предмета [{subject.Name}] (ID={subject.Id}) не найден ни один кабинет. Генерация невозможна.";
                    _logger.LogError(msg);
                    return BadRequest(new { error = msg });
                }
            }

            SchedulePeriodType selectedSchedulePeriodType = classGroupContainers.First().First().Template.PeriodType;
            // Количество рабочих недель выбираем исходя из выбранного периода
            int countWorkWeeks = selectedSchedulePeriodType switch
            {
                SchedulePeriodType.Week => 1,
                SchedulePeriodType.Term => 9, // Прикидываем, что это первая четверть
                SchedulePeriodType.HalfYear => 16, // Прикидываем, что это первое полугодие
                SchedulePeriodType.Year => 34,
                _ => 9
            };

            // Максимальное количество уроков в выбранном периоде
            int maxLessonCountPerPeriod = countWorkWeeks * _maxCountLessonsPerWeek;

            // ID класса, с которым мы сейчас работаем
            int currentClassId = 0;
            ClassGroup currentClassGroup = new();

            // Группированные правила для класса. ClassGroupId является ключём.
            var rulesByClass = new Dictionary<int, List<HoursRule>>();

            // Часы, которые можно съэкономить для класса. ClassGroupId является ключём, а значение - кол-во часов для освобождения на неделе.
            var freedomHours = new Dictionary<int, int>();

            // Перебираем контейнеры с правилами, группированные по КЛАССАМ учеников
            foreach (var classContainers in classGroupContainers)
            {
                currentClassId = classContainers.Key;
                currentClassGroup = allClassGroups.FirstOrDefault(cg => cg.Id == currentClassId) ?? throw new KeyNotFoundException($"Не удалось найти класс по ID = {currentClassId}!");
                rulesByClass[currentClassId] = new List<HoursRule>();

                foreach (var container in classContainers)
                {
                    var rules = JsonSerializer.Deserialize<List<HoursRule>>(container.RulesJson)
                        ?? throw new SerializationException($"[WIZARD-MANUAL-GENERATE] Не удалось десериализовать RulesJson для контейнера ID={container.Id}");
                    rulesByClass[currentClassId].AddRange(rules);
                }

                // Количество всех часов предметов, отведённых на выбранный класс (указанных на странице генерации по шаблону)
                int requiredLessonCount = rulesByClass[currentClassId].Sum(r => r.Hours);

                if (requiredLessonCount > maxLessonCountPerPeriod)
                {
                    string message = $"[WIZARD-MANUAL-GENERATE] Количество часов предметов ({requiredLessonCount}) превышает максимум возможных уроков ({maxLessonCountPerPeriod}) для класса {classContainers.First().ClassGroup.Name}.";
                    _logger.LogError(message);
                    return BadRequest(new
                    {
                        error = message,
                        required = requiredLessonCount,
                        maximum = maxLessonCountPerPeriod
                    });
                }
                else
                {
                    freedomHours[currentClassId] = maxLessonCountPerPeriod - requiredLessonCount;
                    _logger.LogInformation($"[WIZARD-MANUAL-GENERATE] Для класса {classContainers.First().ClassGroup.Name} Количество часов предметов составляет: {requiredLessonCount} при максимуме возможных уроков:{maxLessonCountPerPeriod}.\n\tКоличество часов для 'облегчения страданий': {freedomHours[currentClassId]}");
                }
            }

            // Лимит количества уроков на день для Класса
            var classWorkDays = CalculateDailyLessonLimits(freedomHours);
            // Коллекция ячеек для формирования Уроков по классам
            var baseForLessons = BuildDailyLessonLimits(classWorkDays);
            // 

            var models = new List<ManualGenerateViewModel>();
            foreach (var classGroup in allClassGroups)
            {
                var subjectsForLessons = BuildLessonTasksForClass(currentClassId, rulesByClass[currentClassId], allSubjects, false)
                .GroupBy(s => s.Id)
                .Select(g => (SubjectId: g.Key, Hours: g.Count()))
                .ToList();
                var model = new ManualGenerateViewModel
                {
                    ClassGroup = currentClassGroup,
                    Teachers = allTeachers,
                    Rooms = allRooms,
                    Subjects = allSubjects,
                    CalculatedTimeSlots = baseForLessons[currentClassGroup.Id],
                    SelectedSchedulePeriod = selectedSchedulePeriodType,
                    SubjectHours = subjectsForLessons
                };
                models.Add(model);
            }

            return View("Manual", models);
        }

        [HttpPost]
        public IActionResult SaveManualSchedule([FromBody] ManualScheduleSaveRequest req)
        {
            var classGroup = _context.ClassGroups.FirstOrDefault(c => c.Id == req.ClassId)
                ?? throw new KeyNotFoundException("Класс не найден");

            foreach (var lesson in req.Lessons)
            {
                var timeSlot = _context.TimeSlots.First(ts =>
                    ts.DayOfWeek == lesson.Day &&
                    ts.Period == lesson.Period);

                _context.Lessons.Add(new Lesson
                {
                    ClassGroupId = req.ClassId,
                    SubjectId = lesson.SubjectId,
                    TeacherId = lesson.TeacherId,
                    RoomId = lesson.RoomId,
                    TimeSlotId = timeSlot.Id
                });
            }

            _context.SaveChanges();

            return Ok();
        }
        public class ManualScheduleSaveRequest
        {
            public int ClassId { get; set; }
            public string Period { get; set; } = string.Empty;
            public List<ManualLessonDto> Lessons { get; set; } = new();
        }

        public class ManualLessonDto
        {
            /// <summary>
            /// День недели
            /// </summary>
            /// <remarks>Ожидаем, что работать будем только в будние дни, т.е. ПН-ПТ</remarks>
            [DisplayName("День недели")]
            public Day Day { get; set; }
            public int Period { get; set; }
            public int SubjectId { get; set; }
            public Guid TeacherId { get; set; }
            public int RoomId { get; set; }
        }


        /// <summary>
        /// Запуск минимальной генерации расписания на основе сохранённых HoursRule.
        /// </summary>
        [HttpPost]
        public IActionResult WizardTemplateMinimumGenerate([FromBody] int templateId)
        {
            var classGroupContainers = _context.LessonRuleContainers
                .Where(c => c.TemplateId == templateId)
                .Include(c => c.ClassGroup)
                .Include(c => c.Subject)
                .Include(c => c.Template)
                .OrderBy(c => c.ClassGroup.Grade)
                    .ThenBy(c => c.ClassGroup.Letter)
                .GroupBy(ar => ar.ClassGroupId)
                .ToList();

            var allRooms = _context.Rooms.Include(r => r.AssignedSubjects).ToList();
            var allTeachers = _context.Teachers.Include(t => t.User).Include(t => t.Subjects).ToList();
            var allSubjects = _context.Subjects.ToList();
            var allClassGroups = _context.ClassGroups.ToList();

            if (!classGroupContainers.Any())
                throw new KeyNotFoundException($"[WIZARD-GENERATE-MINIMUM] Не найдены правила для выбранного шаблона (ID = {templateId}) в БД");

            // Проверяем, что у каждого предмета есть хотя бы один кабинет
            foreach (var subject in allSubjects)
            {
                bool hasRoom = allRooms.Any(r => r.AssignedSubjects.Any(s => s.Id == subject.Id));
                if (!hasRoom)
                {
                    string msg = $"[WIZARD-GENERATE-MINIMUM] Для предмета [{subject.Name}] (ID={subject.Id}) не найден ни один кабинет. Генерация невозможна.";
                    _logger.LogError(msg);
                    return BadRequest(new { error = msg });
                }
            }


            // Количество рабочих недель выбираем исходя из выбранного периода
            int countWorkWeeks = classGroupContainers.First().First().Template.PeriodType switch
            {
                SchedulePeriodType.Week => 1,
                SchedulePeriodType.Term => 9, // Прикидываем, что это первая четверть
                SchedulePeriodType.HalfYear => 16, // Прикидываем, что это первое полугодие
                SchedulePeriodType.Year => 34,
                _ => 9
            };

            // Максимальное количество уроков в выбранном периоде
            int maxLessonCountPerPeriod = countWorkWeeks * _maxCountLessonsPerWeek;

            // ID класса, с которым мы сейчас работаем
            int currentClassId = 0;
            ClassGroup currentClassGroup = new();

            // Группированные правила для класса. ClassGroupId является ключём.
            var rulesByClass = new Dictionary<int, List<HoursRule>>();

            // Часы, которые можно съэкономить для класса. ClassGroupId является ключём, а значение - кол-во часов для освобождения на неделе.
            var freedomHours = new Dictionary<int, int>();

            // Перебираем контейнеры с правилами, группированные по КЛАССАМ учеников
            foreach (var classContainers in classGroupContainers)
            {
                currentClassId = classContainers.Key;
                currentClassGroup = allClassGroups.FirstOrDefault(cg => cg.Id == currentClassId) ?? throw new KeyNotFoundException($"Не удалось найти класс по ID = {currentClassId}!");
                rulesByClass[currentClassId] = new List<HoursRule>();

                foreach (var container in classContainers)
                {
                    var rules = JsonSerializer.Deserialize<List<HoursRule>>(container.RulesJson)
                        ?? throw new SerializationException($"[WIZARD-GENERATE-MINIMUM] Не удалось десериализовать RulesJson для контейнера ID={container.Id}");
                    rulesByClass[currentClassId].AddRange(rules);
                }

                // Количество всех часов предметов, отведённых на выбранный класс (указанных на странице генерации по шаблону)
                int requiredLessonCount = rulesByClass[currentClassId].Sum(r => r.Hours);

                if (requiredLessonCount > maxLessonCountPerPeriod)
                {
                    string message = $"[WIZARD-GENERATE-MINIMUM] Количество часов предметов ({requiredLessonCount}) превышает максимум возможных уроков ({maxLessonCountPerPeriod}) для класса {classContainers.First().ClassGroup.Name}.";
                    _logger.LogError(message);
                    return BadRequest(new
                    {
                        error = message,
                        required = requiredLessonCount,
                        maximum = maxLessonCountPerPeriod
                    });
                }
                else
                {
                    freedomHours[currentClassId] = maxLessonCountPerPeriod - requiredLessonCount;
                    _logger.LogInformation($"[WIZARD-GENERATE-MINIMUM] Для класса {classContainers.First().ClassGroup.Name} Количество часов предметов составляет: {requiredLessonCount} при максимуме возможных уроков:{maxLessonCountPerPeriod}.\n\tКоличество часов для 'облегчения страданий': {freedomHours[currentClassId]}");
                }
            }

            // Лимит количества уроков на день для Класса
            var classWorkDays = CalculateDailyLessonLimits(freedomHours);
            // Коллекция ячеек для формирования Уроков по классам
            var baseForLessons = BuildDailyLessonLimits(classWorkDays);

            // Группированная по Классу коллекция Уроков для итогового расписания
            var classLessons = new Dictionary<int, List<Lesson>>();

            //Группированный список связей Класс, Предмет+Учитель+Кабинет.
            var suitableTeachers = new List<SubjectTeacherRoom>();

            try
            {
                foreach (int classId in rulesByClass.Keys)
                {
                    currentClassId = classId;
                    currentClassGroup = allClassGroups.FirstOrDefault(cg => cg.Id == currentClassId)
                        ?? throw new KeyNotFoundException($"Не удалось найти класс по ID = {currentClassId}!");

                    var currentTimeSlots = baseForLessons[currentClassId]
                        .OrderBy(_ => Guid.NewGuid())
                        .ToList();

                    var subjectsForLessons = BuildLessonTasksForClass(currentClassId, rulesByClass[currentClassId], allSubjects);

                    if (!classLessons.ContainsKey(currentClassId))
                        classLessons[currentClassId] = new List<Lesson>();

                    foreach (var currentSubject in subjectsForLessons)
                    {
                        var matchingTeachers = allTeachers
                            .Where(t => t.Subjects.Any(s => s.Id == currentSubject.Id))
                            .ToList();

                        var matchingRooms = allRooms
                            .Where(r => r.AssignedSubjects.Any(s => s.Id == currentSubject.Id))
                            .ToList();

                        if (!matchingTeachers.Any())
                        {
                            string msg = $"[WIZARD-GENERATE-MINIMUM] Не найден ни один учитель для предмета [{currentSubject.Name}] в классе {currentClassGroup.Name}.";
                            _logger.LogError(msg);
                            return BadRequest(new { error = msg });
                        }

                        if (!matchingRooms.Any())
                        {
                            string msg = $"[WIZARD-GENERATE-MINIMUM] Не найден ни один кабинет для предмета [{currentSubject.Name}] в классе {currentClassGroup.Name}.";
                            _logger.LogError(msg);
                            return BadRequest(new { error = msg });
                        }

                        SubjectTeacherRoom? assignment = null;

                        foreach (var slot in currentTimeSlots)
                        {
                            assignment = CheckTeachRoomBusies(
                                currentClassId,
                                currentClassGroup,
                                currentSubject,
                                slot,
                                classLessons,
                                suitableTeachers,
                                in matchingTeachers,
                                in matchingRooms);

                            if (assignment != null)
                            {
                                var lesson = new Lesson
                                {
                                    ClassGroupId = currentClassId,
                                    ClassGroup = currentClassGroup,
                                    SubjectId = currentSubject.Id,
                                    Subject = currentSubject,
                                    TeacherId = assignment.Teacher.UserId,
                                    Teacher = assignment.Teacher,
                                    RoomId = assignment.Room.Id,
                                    Room = assignment.Room,
                                    TimeSlotId = slot.Id,
                                    TimeSlot = slot
                                };

                                classLessons[currentClassId].Add(lesson);
                                break;
                            }
                        }

                        if (assignment == null)
                        {
                            StringBuilder stringBuilder = new();
                            string msg = $"[WIZARD-GENERATE-MINIMUM] Не удалось разместить предмет [{currentSubject.Name}] в классе {currentClassGroup.Name}.";
                            stringBuilder.AppendLine(msg);

                            stringBuilder.AppendLine("На текущий момент вот список расписания:");
                            foreach (var classesId in classLessons.Keys)
                            {
                                stringBuilder.AppendLine($"\tКласс {classLessons[classesId].First().ClassGroup.Name}:");
                                var currentDay = classLessons[classesId].First().TimeSlot.DayOfWeek;

                                foreach (var classLesson in classLessons[classesId].OrderBy(cl => cl.TimeSlot.DayOfWeek).ThenBy(cl => cl.TimeSlot.Period))
                                {
                                    if (currentDay != classLesson.TimeSlot.DayOfWeek)
                                    {
                                        currentDay = classLesson.TimeSlot.DayOfWeek;
                                        stringBuilder.AppendLine($"\t\t {currentDay.GetDisplayName()}:");
                                        stringBuilder.AppendLine($"\t\t\t {classLesson.TimeSlot.Period}. {classLesson.Subject.Name} | {classLesson.Teacher.User.GetFIO} | {classLesson.Room.RoomNumber}");
                                    }
                                    else
                                    {
                                        stringBuilder.AppendLine($"\t\t\t {classLesson.TimeSlot.Period}. {classLesson.Subject.Name} | {classLesson.Teacher.User.GetFIO} | {classLesson.Room.RoomNumber}");
                                    }
                                }
                            }

                            _logger.LogError(stringBuilder.ToString());
                            return BadRequest(new { error = msg });
                        }
                    }
                    _logger.LogInformation($"[WIZARD-GENERATE-MINIMUM] Для класса {currentClassGroup.Name} расписание сгенерировано!.");
                }

                // Сохраняем все сгенерированные уроки
                foreach (var kvp in classLessons)
                {
                    foreach (var lesson in kvp.Value)
                        _context.Lessons.Add(lesson);
                }
                _context.SaveChanges();
            }
            catch (Exception)
            {
                _logger.LogError($"[WIZARD-GENERATE-MINIMUM] Возникла ошибка при генерации расписания для класса {currentClassGroup.Name}");
                throw;
            }
            return Ok();
        }

        /// <summary>
        /// Расчёт количества уроков на день для классов в соответствии со свободными часами у классов.
        /// </summary>
        /// <param name="freedomHours">Ранее расчитанные "Свободные часы" где КЛЮЧ - ID класса</param>
        /// <returns>Словарь Класс -> Словарь День->->Количество уроков в день</returns>
        private Dictionary<int, Dictionary<Day, int>> CalculateDailyLessonLimits(Dictionary<int, int> freedomHours)
        {
            var rnd = new Random();

            // Массив учебных дней.
            var days = Enum.GetValues<Day>();

            var result = new Dictionary<int, Dictionary<Day, int>>();

            // Расчёт загруженности по дням для классов
            foreach (var kvp in freedomHours)
            {
                int classId = kvp.Key;
                int free = kvp.Value;

                // базовый лимит — 6 уроков в день
                var limits = days.ToDictionary(d => d, d => _maxCountLessonsPerDay);

                while (free > 0)
                {
                    // Находим текущий максимум
                    int currentMax = limits.Values.Max();

                    // Берём только те дни, где лимит == максимум
                    var candidateDays = limits
                        .Where(k => k.Value == currentMax)
                        .OrderBy(k => Array.IndexOf(days, k.Key))
                        .Select(k => k.Key)
                        .ToList();

                    // Выбираем случайный день среди кандидатов
                    var chosenDay = candidateDays[rnd.Next(candidateDays.Count)];

                    // Уменьшаем лимит
                    limits[chosenDay] -= 1;
                    free--;
                }
                result[classId] = limits;
            }

            return result;
        }

        /// <summary>
        /// Генерация <see cref="TimeSlot">"Ячеек"</see> для уроков <see cref="ClassGroup">классов</see> в связи со "свободными часами"
        /// </summary>
        /// <param name="dailyLimits"></param>
        /// <returns>Словарь <see cref="ClassGroup">Класс</see>-> Список <see cref="TimeSlot"/></returns>
        private Dictionary<int, List<TimeSlot>> BuildDailyLessonLimits(Dictionary<int, Dictionary<Day, int>> dailyLimits)
        {
            var allSlots = _context.TimeSlots
    .OrderBy(ts => ts.DayOfWeek)
        .ThenBy(ts => ts.Period)
    .ToList();

            var slotsByDay = allSlots
                .GroupBy(ts => ts.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            var classSlots = new Dictionary<int, List<TimeSlot>>();

            foreach (var kvp in dailyLimits)
            {
                int classId = kvp.Key;
                var limits = kvp.Value;

                var list = new List<TimeSlot>();

                foreach (var day in limits.Keys)
                {
                    int limit = limits[day];

                    list.AddRange(slotsByDay[day].Take(limit));
                }

                classSlots[classId] = list;
            }

            return classSlots;
        }

        /// <summary>
        /// Проверяет возможность размещения урока (Предмет+Учитель+Кабинет) в указанной ячейке расписания.
        /// </summary>
        /// <param name="classId">Идентификатор класса.</param>
        /// <param name="subject">Предмет, который требуется разместить.</param>
        /// <param name="slot">Ячейка расписания (день+номер урока).</param>
        /// <param name="matchingTeachers">Список подходящих учителей по специализации.</param>
        /// <param name="matchingRooms">Список подходящих кабинетов по специализации.</param>
        /// <param name="lessons">Уже сформированные уроки по классам.</param>
        /// <returns>
        /// Объект <see cref="SubjectTeacherRoom"/> при успешном подборе.
        /// null — если ни один учитель/кабинет не подходит.
        /// </returns>
        private SubjectTeacherRoom? CheckTeachRoomBusies(
    int classId,
    ClassGroup classGroup,
    Subject subject,
    TimeSlot slot,
    Dictionary<int, List<Lesson>> lessons,
    List<SubjectTeacherRoom> suitableTeachers,
    in List<Teacher> matchingTeachers,
    in List<Room> matchingRooms)
        {
            // 1. Проверка занятости класса
            if (lessons.TryGetValue(classId, out var classLessonList))
            {
                // 1.0. Слот занят
                if (classLessonList.Any(l => l.TimeSlotId == slot.Id))
                {
                    //_logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Класс ({classGroup.Name}) в Ячейке ({slot.TimeSlotString}) занят - ищем дальше...");
                    return null;
                }

                // 1.1. В этот день уже есть такой предмет
                if (classLessonList.Any(l =>
                    l.SubjectId == subject.Id &&
                    l.TimeSlot.DayOfWeek == slot.DayOfWeek))
                {
                    //_logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] В выбранный день ({slot.DayOfWeek.GetDisplayName()}) у класса ({classGroup.Name}) уже есть предмет ({subject.Name}) - ищем дальше...");
                    return null;
                }
            }

            // 2. Ищем существующую связь для класса и предмета
            var existingLink = suitableTeachers
                .FirstOrDefault(st => st.ClassId == classId && st.Subject.Id == subject.Id);

            if (existingLink != null)
            {
                if (IsTeacherBusy(existingLink.Teacher.UserId, slot.Id, lessons))
                {
                    _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Подобранная связь учитель+предмет ([{existingLink.Teacher.User.GetFIO}]+[{existingLink.Subject.Name}]) для класса {classGroup.Name} в слот {slot.TimeSlotString} недоступны...");
                    return null;
                }

                if (IsRoomBusy(existingLink.Room.Id, slot.Id, lessons))
                {
                    _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Выбранный кабинет {existingLink.Room.RoomNumber} в ячейку {slot.TimeSlotString} уже занят");
                    return null;
                }

                return existingLink;
            }

            // 3. Выбор учителя
            Teacher selectedTeacher;

            var linksForSubject = suitableTeachers
                .Where(st => st.Subject.Id == subject.Id)
                .ToList();

            if (linksForSubject.Any())
            {
                var teacherLoad = linksForSubject
                    .GroupBy(st => st.Teacher.UserId)
                    .Select(g => new { Teacher = g.First().Teacher, Count = g.Count() })
                    .OrderBy(x => x.Count)
                    .ToList();

                selectedTeacher = teacherLoad.First().Teacher;

                _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Выбран учитель {selectedTeacher.User.GetFIO} для предмета {subject.Name}.");
            }
            else
            {
                var teacherLoad = matchingTeachers
                    .Select(t => new
                    {
                        Teacher = t,
                        Count = suitableTeachers.Count(st => st.Teacher.UserId == t.UserId)
                    })
                    .OrderBy(x => x.Count)
                    .ToList();

                selectedTeacher = teacherLoad.First().Teacher;

                _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Выбран учитель {selectedTeacher.User.GetFIO} для предмета {subject.Name}.");
            }

            if (IsTeacherBusy(selectedTeacher.UserId, slot.Id, lessons))
            {
                _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Учитель {selectedTeacher.User.GetFIO} для слота {slot.TimeSlotString} занят");
                return null;
            }

            // 4. Выбор кабинета
            Room selectedRoom;

            var teacherPreferredRoom = selectedTeacher.AssignedRooms?
    .FirstOrDefault(r => r.AssignedSubjects?.Any(s => s.Id == subject.Id) == true);


            if (teacherPreferredRoom != null)
            {
                selectedRoom = teacherPreferredRoom;
                _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Предпочтительный кабинет учителя: {selectedRoom.RoomNumber}.");
            }
            else
            {
                var subjectPreferredRoom = matchingRooms.FirstOrDefault();
                if (subjectPreferredRoom != null)
                {
                    selectedRoom = subjectPreferredRoom;
                    _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Предпочтительный кабинет предмета: {selectedRoom.RoomNumber}.");
                }
                else
                {
                    selectedRoom = matchingRooms.FirstOrDefault()
                        ?? throw new KeyNotFoundException($"[WIZARD-CHECKTEACHROOMBUSIES] Не удалось найти кабинет для предмета {subject.Name}");
                }
            }

            if (IsRoomBusy(selectedRoom.Id, slot.Id, lessons))
            {
                _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Кабинет {selectedRoom.RoomNumber} занят в слот {slot.TimeSlotString}.");

                return null;
            }

            // 5. Создаём новую связь
            var newLink = new SubjectTeacherRoom
            {
                ClassId = classId,
                Subject = subject,
                Teacher = selectedTeacher,
                Room = selectedRoom
            };

            suitableTeachers.Add(newLink);

            _logger.LogDebug($"[WIZARD-CHECKTEACHROOMBUSIES] Новая связь класс+предмет+учитель+кабинет ([{classGroup.Name}]+[{subject.Name}]+[{selectedTeacher.User.GetFIO}]+[{selectedRoom.RoomNumber}]) создана");

            return newLink;
        }

        private bool IsTeacherBusy(Guid teacherId, int slotId, Dictionary<int, List<Lesson>> lessons)
        {
            foreach (var classLessons in lessons.Values)
            {
                foreach (var lesson in classLessons)
                {
                    if (lesson.TeacherId == teacherId && lesson.TimeSlotId == slotId)
                    {
                        // Диагностика
                        _logger.LogDebug($"[WIZARD-CHECKTEACHER] Учитель ({lesson.Teacher.User.GetFIO}) занят: В выбранный слот ({lesson.TimeSlot.TimeSlotString}) обучает класс ({lesson.ClassGroup.Name}) предмету ({lesson.Subject.Name})");
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsRoomBusy(int roomId, int slotId, Dictionary<int, List<Lesson>> lessons)
        {
            foreach (var classLessons in lessons.Values)
            {
                foreach (var lesson in classLessons)
                {
                    if (lesson.RoomId == roomId && lesson.TimeSlotId == slotId)
                    {
                        // Диагностика
                        _logger.LogDebug($"[WIZARD-CHECKROOM] Кабинет ({lesson.Room.RoomNumber}) занят: В выбранный слот ({lesson.TimeSlot.TimeSlotString}) обучается класс ({lesson.ClassGroup.Name}) предмету ({lesson.Subject.Name}) учителем ({lesson.Teacher.User.GetFIO})");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Формируем перемешанный список предметов для выбранного класса на основании часов (они указаны в правиле <see cref="HoursRule"/>).
        /// Каждая задача соответствует одному уроку предмета.
        /// </summary>
        /// <param name="classId">Идентификатор класса.</param>
        /// <param name="rules">Список правил количества часов по предметам.</param>
        /// <returns>Случайно перемешанный список  предметов.</returns>
        private List<Subject> BuildLessonTasksForClass(int classId, List<HoursRule> rules, in List<Subject> allSubjects, bool isNeededMix = true)
        {
            var tasks = new List<Subject>();

            foreach (var rule in rules)
            {
                for (int i = 0; i < rule.Hours; i++)
                {
                    Subject subject = allSubjects.Find(s => s.Id == rule.SubjectId)
                        ??
                        throw new KeyNotFoundException($"[WIZARD-BUILDLESSONTASKSFORCLASS] Не удалось найти предмет по ID {rule.SubjectId}");
                    tasks.Add(subject);
                }
            }

            if (isNeededMix)
            {
                return tasks;
            }
            else
            {

                // Перемешиваем задачи для случайного распределения предметов
                return tasks.OrderBy(_ => Guid.NewGuid()).ToList();
            }
        }

        /// <summary>
        /// Необходимая DTO для связи Предмет+Учитель+Кабинет по Классу при генерации рассписания
        /// </summary>
        private class SubjectTeacherRoom
        {
            public int ClassId { get; set; }
            public Subject Subject { get; set; } = null!;
            public Teacher Teacher { get; set; } = null!;
            public Room Room { get; set; } = null!;
        }
    }
}
