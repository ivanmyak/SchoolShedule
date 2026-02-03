using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolShedule.Data;
using System.Text.RegularExpressions;

namespace SchoolShedule.Providers
{
    public static class DbProviderFactory
    {
        public static void Configure(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            var providers = new Dictionary<string, Action>
            {
                ["Postgres"] = () => ConfigurePostgres(cfg, services, logger),
                ["MSSQL"] = () => ConfigureMssql(cfg, services, logger),
                // в будущем можно добавить ещё работу с иными БД (Oracle, MySQL и т.д.)
            };

            foreach (var pv in providers)
            {
                try
                {
                    pv.Value();
                    logger.LogInformation("[DB] {Provider} настроен успешно!.", pv.Key);
                    return; // первый успешный — используем
                }
                catch (Exception ex)
                {
                    logger.LogWarning("[DB] {Provider} failed: {Message}", pv.Key, ex.Message);
                }
            }

            throw new InvalidOperationException("[DB] Отсутствуют 'работающие' провайдеры БД!");

        }

        private static void ConfigurePostgres(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            var raw = cfg.GetConnectionString("PostgresDB") ?? throw new InvalidOperationException("Нет строки PostgresDB!");
            var conn = getGoodConnectString(raw, cfg, logger);

            using var c = new Npgsql.NpgsqlConnection(conn);
            c.Open(); // проверка

            services.AddDbContext<SheduleDbContext>(opt => opt.UseNpgsql(conn));
        }

        private static void ConfigureMssql(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            var raw = cfg.GetConnectionString("MSSQLDB") ?? throw new InvalidOperationException("Нет строки MSSQLDB!");
            //Танцы с бубном, чтобы БД лежала в папке c проектом 
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", $"{cfg["DB_NAME"]}.mdf");

            var goodConn = getGoodConnectString(raw.Replace("${MSSQL_DB_FILE_LOCATION}", dbPath), cfg, logger);

            services.AddDbContext<SheduleDbContext>(opt => opt.UseSqlServer(goodConn));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SheduleDbContext>();
            db.Database.EnsureCreated();
        }

        /// <summary>
        /// Преобразование строки подключения с кодом переменных в правильную строку
        /// </summary>
        /// <param name="raw">Строка подключения с "кодами переменных"</param>
        /// <returns>Правильная строка подключения</returns>
        private static string getGoodConnectString(string raw, IConfiguration cfg, ILogger logger)
        {

            if (string.IsNullOrEmpty(raw))
            {
                logger.LogError("[DB] Строка подключения пуста!");
                throw new ArgumentException("[DB] Строка подключения пуста!");
            }


            var result = raw; var missing = new List<string>();
            var matches = Regex.Matches(raw, @"\$\{(?<var>[A-Za-z0-9_]+)\}");

            foreach (Match m in matches)
            {
                var varName = m.Groups["var"].Value;
                var value = Environment.GetEnvironmentVariable(varName) ?? cfg[varName];
                if (string.IsNullOrEmpty(value))
                    missing.Add(varName);
                else
                    result = result.Replace(m.Value, value);
            }
            if (missing.Count > 0)
            {
                logger.LogError("[DB] Не найдены значения для переменных: {Vars}", string.Join(", ", missing));
                throw new InvalidOperationException($"Отсутствуют переменные: {string.Join(", ", missing)}");
            }
            return result;
        }
    }



}
