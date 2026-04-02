using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Interfaces;
using SchoolShedule.Providers;
using SchoolShedule.Services;

namespace SchoolShedule
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация: сначала secrets, потом env
            builder.Configuration
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();

            // Подключаю адаптер "Логирование"
            LoggingProviderFactory.Configure(builder.Logging, builder.Configuration);
            var logger = LoggerFactory.Create(logging =>
                LoggingProviderFactory.Configure(logging, builder.Configuration))
              .CreateLogger<Program>();

            // Подключаю адаптер "БД"
            DbProviderFactory.Configure(builder.Configuration, builder.Services, logger);

            // Identity + EF Core
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
            })
                .AddEntityFrameworkStores<SheduleDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddAuthorization();

            // DI-связкb интерфейса и сервиса 
            builder.Services.AddScoped<ITranslit, TranslitService>();
            builder.Services.AddScoped<ICredentialsGenerator, CredentialsGeneratorService>();

            // Скервисы по-умолчанию
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Проверка подключения к нашей Базе и применение миграций 
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SheduleDbContext>();
                try
                {
                    if (builder.Environment.IsDevelopment())
                    { // В DEV - использую EnsureCreated — чтобы быстро поднять пустую базу 
                        if (db.Database.EnsureCreated())
                            logger.LogInformation("[DB-TEST] База создана через EnsureCreated.");
                        else
                            logger.LogInformation("[DB-TEST] База уже существует.");
                    }
                    else
                    {
                        // В PROD — всегда миграции 
                        db.Database.Migrate();
                        logger.LogInformation("[DB-TEST] Миграции применены успешно.");
                    }
                    //Проверочка внутряночки
                    if (!db.Users.Any() && !db.Subjects.Any())
                        logger.LogInformation("[DB-TEST] База пустая — можно запускать DbInitializer.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[DB-TEST] Ошибка применения миграций!");
                    throw;
                }
            }

            app.UseExceptionHandler("/Home/Error");

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
