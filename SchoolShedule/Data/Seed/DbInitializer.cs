using Microsoft.AspNetCore.Identity;
using SchoolShedule.Entities;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace SchoolShedule.Data.Seed
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(SheduleDbContext context, ILogger logger)
        {
            string seedPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "seed.json");
            if (!File.Exists(seedPath))
            {
                logger.LogError($"[DB] Отсутствует файл-зерно по пути: {seedPath}");
                //throw new DataException("В БД уже присутствуют значения - инициализация 'зерном' отменена!");
                return;
            }

            if (context.Users.Count() > 1 || context.Subjects.Any() || context.ClassGroups.Any())
            {
                // если база уже заполнена - сообщаем об неудаче
                logger.LogWarning("[DB] В БД уже присутствуют значения - инициализация 'зерном' отменена!");
                //throw new DataException("В БД уже присутствуют значения - инициализация 'зерном' отменена!");
                return;

            }
            var json = await File.ReadAllTextAsync(seedPath);
            var seedData = JsonSerializer.Deserialize<SeedData>(json);

            if (seedData == null)
            {
                logger.LogError("Файл с 'зерном' отсутствует, пуст или не удалось обработать!");
                //throw new FileLoadException("Файл с 'зерном' отсутствует, пуст или не удалось обработать!");
                return;

            }
            List<User> users = new();
            List<Teacher> teachers = new();
            List<Student> students = new();

            List<Subject> subjects = new();
            List<ClassGroup> classGroups = new();
            List<Room> rooms = new();
            List<TimeSlot> timeslots = new();
            List<Lesson> lessons = new();
            //string[] tryseeds = ["users", "teachers", "students", "subjects", "classGroups", "rooms", "timeslots", "lessons"];
            string seedentitiesstatus = "users";
            // Обрабатываем json-сущности и добавляем в БД
            try
            {

                // Users 
                users.AddRange(seedData.Users);
                context.AddRange(users);

                // Subjects
                seedentitiesstatus = nameof(Subject);
                subjects.AddRange(seedData.Subjects);
                context.AddRange(subjects);

                // Teachers
                seedentitiesstatus = nameof(Teacher);
                foreach (var teacherSeed in seedData.Teachers)
                {
                    var teacher = new Teacher
                    {
                        UserId = teacherSeed.UserId,
                        User = users
                        .First(u =>
                            u.Id == teacherSeed.UserId),
                        Subjects = subjects
                        .Where(s =>
                            teacherSeed.Subjects.Contains(s.Code))
                        .ToList()

                    };
                    teachers.Add(teacher);
                }
                context.AddRange(teachers);

                // ClassGroups
                seedentitiesstatus = nameof(classGroups);
                classGroups.AddRange(seedData.ClassGroups);
                context.AddRange(classGroups);

                // Students
                seedentitiesstatus = nameof(Student);
                foreach (var studentSeed in seedData.Students)
                {
                    var student = new Student
                    {
                        UserId = studentSeed.UserId,
                        User = users.First(u =>
                            u.Id == studentSeed.UserId),
                        ClassGroup = classGroups
                            .First(c =>
                                c.Grade == studentSeed.Grade && c.Letter == studentSeed.Letter[0])

                    };
                    students.Add(student);
                }
                context.AddRange(students);

                // Rooms
                seedentitiesstatus = nameof(Room);
                rooms.AddRange(seedData.Rooms);
                context.AddRange(rooms);

                // TimeSlots
                seedentitiesstatus = nameof(TimeSlot);
                timeslots.AddRange(seedData.TimeSlots);
                context.AddRange(timeslots);

                // Lessons
                seedentitiesstatus = nameof(Lesson);
                foreach (var l in seedData.Lessons)
                {
                    var lesson = new Lesson
                    {
                        Subject = subjects.First(s => s.Code == l.SubjectCode),
                        Teacher = teachers.First(t => t.UserId == l.TeacherId),
                        ClassGroup = classGroups.First(c => c.Grade == l.Grade && c.Letter == l.Letter[0]),
                        Room = rooms.First(r => $"{r.Floor}-{r.Number}" == l.RoomNumber),
                        TimeSlot = timeslots.First(ts => (int)ts.DayOfWeek == l.DayOfWeek && ts.Period == l.Period)
                    };
                    lessons.Add(lesson);
                }
                context.AddRange(lessons);


                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                logger.LogError($"[SEED] Возникла ошибка в иницализации при работе с {seedentitiesstatus}");
                throw;
            }


        }

        public static async Task InitAdminAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
        {
            var user = await userManager.FindByEmailAsync("user1@guestmail.com");
            if (user != null)
            {
                logger.LogWarning("[DB] В БД уже присутствует Администратор - инициализация отменена!");
                //throw new DataException("В БД уже присутствует Администратор - инициализация отменена!");
                return;
            }
            user = new User()
            {
                UserName = "SuperAdmiral",
                FirstName = "Иван",
                MiddleName = "Иванович",
                LastName = "Иванов",
                Email = "superadmin@admin.com"

            };

            var adminRole = await roleManager.FindByNameAsync("Administrator");
            if (adminRole != null)
            {
                logger.LogWarning("[DB] В БД уже присутствует Администратор! Выполянем проверку это 'умолчаемый' ли пользователь или кто-то другой");
                if (await userManager.IsInRoleAsync(user, "Administrator"))
                {
                    logger.LogWarning("[DB] пользователь в БД с ролью администратор уже присутствует - ничего не делаем");
                    return;
                    //throw new DataException("В БД уже присутствует Администратор - инициализация отменена!");
                }
            }
            adminRole = new() { Name = "Administrator" };
            await roleManager.CreateAsync(adminRole);

            var result = await userManager.CreateAsync(user, "Password123!");

            if (result.Succeeded) // И, после создания Юзверя и Роли - объединяем их (PanPineappleApplePan!)
            {
                await userManager.AddToRoleAsync(user, adminRole.Name ?? "Administrator");
                return;
            }

            else
            {
                logger.LogError("[DB] Не удалось создать запланированного администратора!");
                throw new DataException("[DB] Не удалось создать запланированного администратора!");
            }

        }
    }

    // DTO для десериализации JSON
    public class SeedData
    {
        public List<User> Users { get; set; } = new();
        public List<TeacherSeed> Teachers { get; set; } = new();
        public List<StudentSeed> Students { get; set; } = new();
        public List<ClassGroup> ClassGroups { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<LessonSeed> Lessons { get; set; } = new();
    }

    public class LessonSeed
    {
        public string SubjectCode { get; set; } = default!;
        public Guid TeacherId { get; set; }
        public int Grade { get; set; }
        public string Letter { get; set; } = default!;
        public string RoomNumber { get; set; } = default!; // "этаж-номер"
        public int DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class StudentSeed
    {
        public Guid UserId { get; set; }
        public int Grade { get; set; }
        public string Letter { get; set; } = default!;
    }
    public class TeacherSeed
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// коды предметов
        /// </summary>
        /// <example>(MATH, PHYS, ...)</example>
        public List<string> Subjects { get; set; } = new();
    }


}
