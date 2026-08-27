using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Entities.Generation;
using SchoolShedule.Entities.Generation.Rules;
using SchoolShedule.Enums;
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
        /// <summary>
        /// Максимум уроков в день.
        /// </summary>
        /// <remarks>Можно высчитывать из имеющихся в БД TimeSlot, но пока закостылил</remarks>
        private static readonly int _maxCountLessonsPerDay = 6;
        /// <summary>
        /// Все "ячейки" для уроков, возможные на неделе
        /// </summary>
        private readonly int _maxCountLessonsPerWeek = _maxCountLessonsPerDay*5;


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

            /// Вычисляем количество максимума всех возможных уроков на неделе по формуле:
            
            /// Полученное количество часов/уроков сравниваем с необходимым (СУММА_ПО_ПРЕДМЕТО-ЧАСЫ)
            /// Если СУММА_ПО_ПРЕДМЕТО-ЧАСЫ > МАКСИМУМА_ВОЗМОЖНЫХ_УРОКОВ - я хз что делать тут.
            /// Возможно этот момент мы можем проверять и показывать ещё на этапе выставления часов по предметам...
            /// ... чтобы админ не навалил часов по предметам больше, чем они физически есть в расписании
            /// Если СУММА_ПО_ПРЕДМЕТО-ЧАСЫ <= МАКСИМУМА_ВОЗМОЖНЫХ_УРОКОВ :
            /// Каким-то образом прикидываем сколько мы можем сделать уроков для этого класса на неделе
            /// (каждый день по 6 уроков, или 4 дня по 6 уроков и один по 5, или 3 по 6 и два по 5 - так и понижать, пока не сравняется)
            /// Из этого - формируем Группированный список ячеек для уроков. Группировка - День недели(?).
            /// Генерация будет чем-то таким:
            /// Формируем случайно-порядковый СТЭК (вариант списка) из необходимых уроков (ПРЕДМЕТ*ЧАСЫ.
            /// Т.е. собираем весь тот список уроков, которые для выбранного класса нужно раскидать по расписанию на неделю)
            /// Формируем некую коллекцию <ЯЧЕЙКА_УРОКА, КАБИНЕТ, ПРЕДМЕТ, УЧИТЕЛЬ, КЛАСС> - тут я не совсем понимаю
            /// Запускаем цикл по Доставанию Предметов из СТЭКА.
            /// Рандомно начинаем "тянуть" ЯЧЕЙКА_УРОКА, ПРЕДМЕТ и, сравнивая с уже имеющимся сгруппированным списком по <ЯЧЕЙКА_УРОКА, КАБИНЕТ, ПРЕДМЕТ, УЧИТЕЛЬ, КЛАСС>
            /// Оценивать, есть ли в данном дне уже этот предмет или же нет.
            /// Есть - берём следующую случайную ячейку и проверка повторяется.
            /// Отсутствует - засовываем эту комбинацию в <ЯЧЕЙКА_УРОКА, КАБИНЕТ, ПРЕДМЕТ, УЧИТЕЛЬ, КЛАСС> и продолжаем цикл.
            //
            /// Это - что касаемо самого первого класса, с которым генерируем. Последующие - почти аналогично,
            /// только проверки при объединении ячейка-предмет будут ещё и смотреть на незанятость в этот период Кабинета+Предмета+Учителя
            //
            /// TODO: здесь будет минимальный генератор:
            /// - по каждому контейнеру
            /// - по каждому HoursRule
            /// - создаём Lesson в нужном количестве

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

            if (!classGroupContainers.Any())
                throw new KeyNotFoundException($"[WIZARD-GENERATE-MINIMUM] Не найдены правила для выбранного шаблона (ID = {templateId}) в БД");

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

            // Группированные правила для класса. ClassGroupId является ключём.
            var rulesByClass = new Dictionary<int, List<HoursRule>>();

            // Часы, которые можно съэкономить для класса. ClassGroupId является ключём, а значение - кол-во часов для освобождения на неделе.
            var freedomHours = new Dictionary<int, int>();

            // Перебираем контейнеры с правилами, группированные по КЛАССАМ учеников
            foreach (var classContainers in classGroupContainers)
            {
                int classId = classContainers.Key;
                rulesByClass[classId] = new List<HoursRule>();

                foreach (var container in classContainers)
                {
                    var rules = JsonSerializer.Deserialize<List<HoursRule>>(container.RulesJson)
                        ?? throw new SerializationException($"Не удалось десериализовать RulesJson для контейнера ID={container.Id}");
                    rulesByClass[classId].AddRange(rules);
                }

                // Количество всех часов предметов, отведённых на выбранный класс (указанных на странице генерации по шаблону)
                int requiredLessonCount = rulesByClass[classId].Sum(r => r.Hours);

                if (requiredLessonCount > maxLessonCountPerPeriod)
                {
                    string message = $"Количество часов предметов ({requiredLessonCount}) превышает максимум возможных уроков ({maxLessonCountPerPeriod}) для класса {classContainers.First().ClassGroup.Name}.";
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
                    freedomHours[classId] = maxLessonCountPerPeriod - requiredLessonCount;
                    _logger.LogInformation($"Для класса {classContainers.First().ClassGroup.Name} Количество часов предметов составялет: {requiredLessonCount} при максимуме возможных уроков:{maxLessonCountPerPeriod}.\n\tКоличество часов для 'облегчения страданий': {freedomHours[classId]}");
                }
            }

            var classWorkDays = CalculateDailyLessonLimits(freedomHours);
            var baseForLessons = BuildDailyLessonLimits(classWorkDays);


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
        /// Генерация <see cref="TimeSlot">"Ячеек"</see> для уроков для <see cref="ClassGroup">классов</see> в связи со "свободными часами"
        /// </summary>
        /// <param name="dailyLimits"></param>
        /// <returns>Словарь Класс-> Список <see cref="TimeSlot"/></returns>
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
    }
}
