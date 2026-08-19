using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.DependencyResolver;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;
using System.ComponentModel.DataAnnotations;
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
                Dictionary<string, User> createdUsers = new();
                List<Subject> createdSubjects = new();
                List<Room> createdRooms = new();


                foreach (var u in seedData.Users)
                {
                    string userName = u.Email ?? await generator.GenerateUserNameAsync(u.FirstName, u.LastName);
                    var user = new User
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        MiddleName = u.MiddleName,
                        UserName = userName,
                        Email = u.Email ?? await generator.GenerateEmailAsync(userName),
                    };

                    var result = await userManager.CreateAsync(user, generator.GeneratePassword(user));

                    if (!result.Succeeded)
                    {
                        logger.LogError($"Ошибка создания пользователя {u.GetFIO}({u.Email}): {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        continue;
                    }
                    createdUsers.Add(user.UserName, user);
                }

                // -----------------------------
                // 3. Создаём предметы
                // -----------------------------
                createdSubjects = seedData.Subjects;
                context.Subjects.AddRange(createdSubjects);
                await context.SaveChangesAsync();

                // -----------------------------
                // 4. Создаём преподавателей
                // -----------------------------
                List<Teacher> teachers = new();

                foreach (var t in seedData.Teachers)
                {
                    var teacher = new Teacher
                    {
                        User = createdUsers[t.UserName],
                        Subjects = createdSubjects.Where(s => t.Subjects.Contains(s.Code)).ToList()
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
                // 6. Кабинеты
                // -----------------------------
                foreach (var r in seedData.Rooms)
                {
                    var room = new Room
                    {
                        Floor = r.Floor,
                        Number = r.Number,
                        AssignedSubjects = createdSubjects.Where(s => r.Subjects.Contains(s.Code)).ToList()
                    };
                    createdRooms.Add(room);
                }
                context.Rooms.AddRange(createdRooms);
                await context.SaveChangesAsync();

                // -----------------------------
                // 7. Слоты уроков
                // -----------------------------
                context.TimeSlots.AddRange(seedData.TimeSlots);
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
        public List<RoomSeed> Rooms { get; set; } = new();
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
        public string UserName { get; set; } = null!;

        /// <summary>
        /// коды предметов
        /// </summary>
        /// <example>(MATH, PHYS, ...)</example>
        public List<string> Subjects { get; set; } = new();
    }

    public class RoomSeed
    {
        /// <summary>
        /// Этаж
        /// </summary>
        [Display(Name = "Этаж")]
        public int Floor { get; set; }

        /// <summary>
        /// Номер кабинета
        /// </summary>
        [Display(Name = "Номер кабинета")]

        public int Number { get; set; }

        /// <summary>
        /// коды предметов
        /// </summary>
        /// <example>(MATH, PHYS, ...)</example>
        public List<string> Subjects { get; set; } = new();
    }


}
