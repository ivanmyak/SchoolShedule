using Microsoft.AspNetCore.Identity;
using SchoolShedule.Entities;
using System.Data;
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

            // Добавляем сущности
            context.Users.AddRange(seedData.Users);
            context.Teachers.AddRange(seedData.Teachers);
            context.Students.AddRange(seedData.Students);
            context.ClassGroups.AddRange(seedData.ClassGroups);
            context.Subjects.AddRange(seedData.Subjects);
            context.Rooms.AddRange(seedData.Rooms);
            context.TimeSlots.AddRange(seedData.TimeSlots);
            context.Lessons.AddRange(seedData.Lessons);

            await context.SaveChangesAsync();
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
        public List<Teacher> Teachers { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<ClassGroup> ClassGroups { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<Lesson> Lessons { get; set; } = new();
    }

}
