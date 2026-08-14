using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SchoolShedule.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SchoolShedule.Providers
{
    public static class DbProviderFactory
    {
        public static void Configure(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            var providers = new Dictionary<string, Func<bool>>
            {
                ["Postgres"] = () => ConfigurePostgres(cfg, services, logger),
                ["MSSQL"] = () => ConfigureMssql(cfg, services, logger),
                // в будущем можно добавить ещё работу с иными БД (Oracle, MySQL и т.д.)
            };

            foreach (var pv in providers)
            {
                try
                {
                    if (!pv.Value())
                    {
                        logger.LogWarning("[DB-PROV] {Provider} не удалось настроить - переходим к следующему!", pv.Key);
                        continue;
                    }

                    logger.LogInformation("[DB-PROV] {Provider} настроен успешно!.", pv.Key);
                    return; // первый успешный(существующую строку) — используем
                }
                catch (Exception ex)
                {
                    logger.LogError("[DB-PROV] {Provider} failed: {Message}", pv.Key, ex.Message);
                }
            }
            logger.LogWarning("[DB-PROV] Отсутствуют 'работающие' провайдеры БД!");
            throw new InvalidOperationException("[DB-PROV] Отсутствуют 'работающие' провайдеры БД!");
        }

        private static bool ConfigurePostgres(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            try
            {
                var conn = cfg.GetConnectionString("PostgresDB") ?? throw new InvalidOperationException("Нет строки PostgresDB!");
                var goodConn = getGoodConnectString(conn, cfg, logger);
                var connBuilder = new NpgsqlConnectionStringBuilder(goodConn);
                connBuilder.Database = "postgres";

                if (TestConnection(connBuilder.ConnectionString, logger, "Postgres"))
                {
                    services.AddDbContext<SheduleDbContext>(opt => opt.UseNpgsql(goodConn));
                    return true;
                }
                else
                    return false;

            }
            catch (Exception)
            {
                logger.LogError("[DB-CONF] Произошла ошибка при настройки PostgresDB");
                throw;
            }

        }

        private static bool ConfigureMssql(IConfiguration cfg, IServiceCollection services, ILogger logger)
        {
            try
            {
                var conn = cfg.GetConnectionString("MSSQLDB")
                                ?? throw new InvalidOperationException("Нет строки MSSQLDB!");
                //Танцы с бубном, чтобы БД лежала в папке c проектом 
                string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", $"{cfg["DB_NAME"]}.mdf");
                var goodConn = getGoodConnectString(conn.Replace("${MSSQL_DB_FILE_LOCATION}", dbPath), cfg, logger);
                var connBuilder = new SqlConnectionStringBuilder(goodConn);
                connBuilder.AttachDBFilename = ""; // убираем, чтобы не искал файл
                connBuilder.InitialCatalog = "master";
                if (TestConnection(connBuilder.ConnectionString, logger, "MSSQL"))
                {
                    services.AddDbContext<SheduleDbContext>(opt => opt.UseSqlServer(goodConn));
                    return true;
                }
                else
                    return false;

            }
            catch (Exception)
            {

                throw;
            }
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
                logger.LogError("[DB-STR] Строка подключения пуста!");
                throw new ArgumentException("[DB-STR] Строка подключения пуста!");
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
                logger.LogError("[DB-STR] Не найдены значения для переменных: {Vars}", string.Join(", ", missing));
                throw new InvalidOperationException($"Отсутствуют переменные: {string.Join(", ", missing)}");
            }
            return result;
        }

        /// <summary>
        /// Проверка успешности подключения к сервису с БД
        /// </summary>
        /// <remarks>Для каждого сервиса - пробуем подключиться не к нужной БД, а к СИСТЕМНОЙ</remarks>
        /// <param name="conn"> Переопределённая строка подключения к сервису с БД</param>
        /// <param name="logger">сервис-логгер</param>
        /// <param name="providerName">Провайдер, который мы сейчас пытаемся настроить</param>
        /// <returns><see langword="true"/> - Удалось подключиться, <see langword="false"/> - нет</returns>
        private static bool TestConnection(string conn, ILogger logger, string providerName)
        {
            try
            {
                using var dbConn = providerName == "Postgres" ? new NpgsqlConnection(conn) as DbConnection : new SqlConnection(conn);
                dbConn.Open();
                logger.LogInformation("[DB-TEST] {Provider} соединение успешно установлено.", providerName);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("[DB-TEST] {Provider} соединение не удалось: {Message}", providerName, ex.Message);
                return false;
            }
        }
    }



}
