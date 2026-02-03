using Microsoft.AspNetCore.Identity;
using SchoolShedule.Data;
using SchoolShedule.Entities;
using SchoolShedule.Providers;

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

            // Подключаю адаптер "БД"
            DbProviderFactory.Configure(builder.Configuration, builder.Services,
            LoggerFactory.Create(logging =>
                LoggingProviderFactory.Configure(logging, builder.Configuration))
              .CreateLogger<Program>()
              );

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

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

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
