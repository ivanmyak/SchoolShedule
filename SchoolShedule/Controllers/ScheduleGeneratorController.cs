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

            var allRooms = _context.Rooms.Include(r=>r.AssignedSubjects).ToList();
            var allTeachers = _context.Teachers.Include(t=>t.User).Include(t => t.Subjects).ToList();
            var allSubjects = _context.Subjects.ToList();
            var allClassGroups = _context.ClassGroups.ToList();

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
                currentClassGroup = allClassGroups.FirstOrDefault(cg => cg.Id == currentClassId) ??  throw new KeyNotFoundException($"Не удалось найти класс по ID = {currentClassId}!"); 
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

            //Группированный список связей Класс, Предмет+Учитель+Кабинет - то, что нужно будет использовать и учитывать при генерации расписания
            //и с каким списком мы будем сравнивать при поиске подходящих учителей.
            // Правило сравнений: при выборе учителя мы смотрим (из нижеиспользуемого списка matchingTeachers)
            // какой учитель уже задействован и сколько раз - выбираем минимально-задействованного из подходящих
            var suitableTeachers = new List<SubjectTeacherRoom>();

            try
            {
                // Цикл идёт по всем классам для формирования РАСПИСАНИЯ
                foreach (int classId in rulesByClass.Keys)
                {
                    // Запоминаем ID класса
                    currentClassId = classId;
                    currentClassGroup = allClassGroups.FirstOrDefault(cg => cg.Id == currentClassId) ?? throw new KeyNotFoundException($"Не удалось найти класс по ID = {currentClassId}!");

                    //Перетасованные ячейки TimeSlot для выбранного класса
                    var currentTimeSlots = baseForLessons[currentClassId].OrderBy(_ => Guid.NewGuid()).ToList();
                    //Перетасованные Предметы для распределения по выбранному классу
                    var subjectsForLessons = BuildLessonTasksForClass(currentClassId, rulesByClass[currentClassId], allSubjects);


                    // Цикл по перетасованным Предметам класса для формирования его расписания
                    foreach (var currentSubject in subjectsForLessons)
                    {
                        // Подходящие учителя
                        List<Teacher> matchingTeachers = allTeachers
                            .Where(t => t.Subjects.Any(s => s.Id == currentSubject.Id))
                            .ToList();

                        // Подходящие кабинеты
                        List<Room> matchingRooms = allRooms.Where(r => r.AssignedSubjects.Any(s => s.Id == currentSubject.Id)).ToList();


                        SubjectTeacherRoom? assignment = null;

                        // Подбираем слот в расписании для данного Предмета
                        foreach (var slot in currentTimeSlots)
                        {
                            assignment = CheckTeachRoomBusies(
                                currentClassId,
                                currentClassGroup,
                                currentSubject,
                                slot,
                                suitableTeachers,
                                in matchingTeachers,
                                in matchingRooms,
                                in classLessons);

                            if (assignment != null)
                            {
                                // создаём Lesson
                                var lesson = new Lesson
                                {
                                    ClassGroupId = currentClassId,
                                    SubjectId = currentSubject.Id,
                                    TeacherId = assignment.Teacher.UserId,
                                    RoomId = assignment.Room.Id,
                                    TimeSlotId = slot.Id
                                };

                                if (!classLessons.ContainsKey(currentClassId))
                                    classLessons[currentClassId] = new List<Lesson>();

                                classLessons[currentClassId].Add(lesson);

                                break;
                            }
                        }

                        if (assignment == null)
                        {
                            _logger.LogError($"[WIZARD-GENERATE-MINIMUM] Не удалось разместить предмет {currentSubject.Name} в классе {currentClassId}");
                            return BadRequest(new { error = $"Не удалось разместить предмет {currentSubject.Name} в классе {currentClassId}" });
                        }
                    }

                }

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
            List<SubjectTeacherRoom> suitableTeachers,
            in List<Teacher> matchingTeachers,
            in List<Room> matchingRooms,
            in Dictionary<int, List<Lesson>> lessons)
        {
            // 1. Проверяем занятость класса
            if (lessons.TryGetValue(classId, out var classLessonList))
            {
                if (classLessonList.Any(l => l.TimeSlotId == slot.Id))
                    return null;
            }

            // 2. Ищем существующую связь для класса и предмета
            var existingLink = suitableTeachers
                .FirstOrDefault(st => st.ClassId == classId && st.Subject.Id == subject.Id);

            if (existingLink != null)
            {
                // Проверяем занятость учителя
                if (lessons.Values.Any(list => list.Any(l => l.TeacherId == existingLink.Teacher.UserId && l.TimeSlotId == slot.Id)))
                    return null;

                // Проверяем занятость кабинета
                if (lessons.Values.Any(list => list.Any(l => l.RoomId == existingLink.Room.Id && l.TimeSlotId == slot.Id)))
                    return null;

                return existingLink;
            }

            // 3. Ищем связи по предмету (для распределения нагрузки)
            var linksForSubject = suitableTeachers
                .Where(st => st.Subject.Id == subject.Id)
                .ToList();

            Teacher selectedTeacher;

            if (linksForSubject.Any())
            {
                // Группируем по учителю и выбираем наименее загруженного
                var teacherLoad = linksForSubject
                    .GroupBy(st => st.Teacher.UserId)
                    .Select(g => new
                    {
                        Teacher = g.First().Teacher,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Count)
                    .ToList();

                selectedTeacher = teacherLoad.First().Teacher;
            }
            else
            {
                // Если связей по предмету нет — выбираем любого подходящего учителя
                selectedTeacher = matchingTeachers.FirstOrDefault() ?? throw new KeyNotFoundException($"Не удалось найти подходящего учителя для класса {classGroup.Name} по предмету {subject.Name} для ячейки {slot.TimeSlotString}");
                if (selectedTeacher == null)
                    return null;
            }

            // Проверяем занятость выбранного учителя
            if (lessons.Values.Any(list => list.Any(l => l.TeacherId == selectedTeacher.UserId && l.TimeSlotId == slot.Id)))
                return null;

            // 4. Выбор кабинета
            Room selectedRoom;

            // 4.1. Предпочитаемые кабинеты учителя
            var teacherPreferredRoom = selectedTeacher.AssignedRooms
                .FirstOrDefault(r => r.AssignedSubjects.Any(s => s.Id == subject.Id));

            if (teacherPreferredRoom != null)
                selectedRoom = teacherPreferredRoom;
            else
            {
                // 4.2. Кабинеты со специализацией по предмету
                var subjectPreferredRoom = matchingRooms.FirstOrDefault();
                if (subjectPreferredRoom != null)
                    selectedRoom = subjectPreferredRoom;
                else
                    selectedRoom = matchingRooms.FirstOrDefault() ?? throw new KeyNotFoundException($"Не удалось найти подходящего кабинета для класса {classGroup.Name} по предмету {subject.Name} для ячейки {slot.TimeSlotString}"); ;
            }

            // Проверяем занятость кабинета
            if (lessons.Values.Any(list => list.Any(l => l.RoomId == selectedRoom.Id && l.TimeSlotId == slot.Id)))
                return null;

            // 5. Формируем новую связь
            var newLink = new SubjectTeacherRoom
            {
                ClassId = classId,
                Subject = subject,
                Teacher = selectedTeacher,
                Room = selectedRoom
            };

            _logger.LogInformation($"[WIZARD-CHECKTEACHROOMBUSIES] добавляем связь ({subject.Name})+({selectedTeacher.User.GetFIO})+({selectedRoom.RoomNumber}) для класса {classGroup.Name}");
            suitableTeachers.Add(newLink);

            return newLink;
        }

        /// <summary>
        /// Формируем перемешанный список предметов для выбранного класса на основании часов (они указаны в правиле <see cref="HoursRule"/>).
        /// Каждая задача соответствует одному уроку предмета.
        /// </summary>
        /// <param name="classId">Идентификатор класса.</param>
        /// <param name="rules">Список правил количества часов по предметам.</param>
        /// <returns>Случайно перемешанный список  предметов.</returns>
        private List<Subject> BuildLessonTasksForClass(int classId, List<HoursRule> rules, in List<Subject> allSubjects)
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

            // Перемешиваем задачи для случайного распределения предметов
            return tasks.OrderBy(_ => Guid.NewGuid()).ToList();
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
