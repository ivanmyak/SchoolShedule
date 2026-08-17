using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.DependencyResolver;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace SchoolShedule.Data.Seed
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(SheduleDbContext context, ILogger logger, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, ICredentialsGenerator generator)
        {
            string seedPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "seed.json");

            if (!File.Exists(seedPath))
            {
                logger.LogError($"[DB] Отсутствует файл-зерно по пути: {seedPath}");
                return;
            }

            // Одна запись обязана быть - это наш Админ. Если есть иные - значит база чем-то инициализирована. Или очищать-удалять базу, или вручную админом добавлять
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
                logger.LogError("[DB] Файл 'seed.json' пуст или повреждён.");
                return;
            }

            try
            {
                // -----------------------------
                // 1. Создаём роли
                // -----------------------------
                string[] roles = { "Administrator", "Teacher", "Student" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new() { Name = role });
                    }
                }

                // -----------------------------
                // 2. Создаём пользователей через UserManager
                // -----------------------------
                Dictionary<Guid, User> createdUsers = new();

                foreach (var u in seedData.Users)
                {
                    var user = new User
                    {
                        Id = u.Id,
                        UserName = u.Email,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        MiddleName = u.MiddleName
                    };

                    var result = await userManager.CreateAsync(user, generator.GeneratePassword(user));

                    if (!result.Succeeded)
                    {
                        logger.LogError($"Ошибка создания пользователя {u.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        continue;
                    }


                    createdUsers.Add(user.Id, user);
                }

                // -----------------------------
                // 3. Создаём предметы
                // -----------------------------
                context.Subjects.AddRange(seedData.Subjects);
                await context.SaveChangesAsync();

                // -----------------------------
                // 4. Создаём преподавателей
                // -----------------------------
                List<Teacher> teachers = new();

                foreach (var t in seedData.Teachers)
                {
                    var teacher = new Teacher
                    {
                        UserId = t.UserId,
                        User = createdUsers[t.UserId],
                        Subjects = context.Subjects.Where(s => t.Subjects.Contains(s.Code)).ToList()
                    };

                    // Назначаем роль
                    await userManager.AddToRoleAsync(teacher.User, "Teacher");
                    teachers.Add(teacher);
                }

                context.Teachers.AddRange(teachers);
                await context.SaveChangesAsync();

                // -----------------------------
                // 5. Создаём классы
                // -----------------------------
                context.ClassGroups.AddRange(seedData.ClassGroups);
                await context.SaveChangesAsync();

                // -----------------------------
                // 6. Создаём студентов
                // -----------------------------
                List<Student> students = new();

                foreach (var s in seedData.Students)
                {
                    var student = new Student
                    {
                        UserId = s.UserId,
                        User = createdUsers[s.UserId],
                        ClassGroup = context.ClassGroups.First(c => c.Grade == s.Grade && c.Letter == s.Letter[0])
                    };

                    await userManager.AddToRoleAsync(student.User, "Student");
                    students.Add(student);
                }

                context.Students.AddRange(students);
                await context.SaveChangesAsync();

                // -----------------------------
                // 7. Кабинеты, слоты, уроки
                // -----------------------------
                context.Rooms.AddRange(seedData.Rooms);
                context.TimeSlots.AddRange(seedData.TimeSlots);
                await context.SaveChangesAsync();

                List<Lesson> lessons = new();

                foreach (var l in seedData.Lessons)
                {
                    lessons.Add(new Lesson
                    {
                        Subject = context.Subjects.First(s => s.Code == l.SubjectCode),
                        Teacher = teachers.First(t => t.UserId == l.TeacherId),
                        ClassGroup = context.ClassGroups.First(c => c.Grade == l.Grade && c.Letter == l.Letter[0]),
                        Room = context.Rooms.First(r => $"{r.Floor}-{r.Number}" == l.RoomNumber),
                        TimeSlot = context.TimeSlots.First(ts => (int)ts.DayOfWeek == l.DayOfWeek && ts.Period == l.Period)
                    });
                }

                context.Lessons.AddRange(lessons);
                await context.SaveChangesAsync();

                logger.LogInformation("[DB] Инициализация успешно завершена.");
            }
            catch (Exception ex)
            {
                logger.LogError($"[SEED] Ошибка при инициализации: {ex.Message}");
                throw;
            }
        }

        public static async Task InitAdminAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
        {
            var user = await userManager.FindByEmailAsync("superadmin@admin.com");
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
