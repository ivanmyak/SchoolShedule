using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace SchoolShedule.Providers
{
    public static class LoggingProviderFactory
    {
        public static void Configure(ILoggingBuilder logging, IConfiguration cfg)
        {
            logging.ClearProviders();

            var providers = cfg.GetSection("LoggingProviders");
            var files = cfg.GetSection("LogFiles");

            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), files["Root"] ?? Path.Combine("Data", "Logs"), DateTime.Now.Year.ToString());

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            int retention = int.TryParse(files["RetentionDays"], out var r) ? r : 180;

            var minLevel = cfg.GetSection("Logging:LogLevel")["SchoolShedule"] ?? "Information";

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minLevel));

            var providerActions = new Dictionary<string, Action>
            {
                ["Console"] = () => ConfigureConsole(loggerConfig),
                ["Admin"] = () => ConfigureAdminFile(loggerConfig, rootPath, retention),
                ["Monitoring"] = () => ConfigureMonitoringFile(loggerConfig, rootPath, retention)
            };

            foreach (var kv in providerActions)
            {
                if (providers.GetValue<bool>(kv.Key))
                    kv.Value();
            }

            Log.Logger = loggerConfig.CreateLogger();
            logging.AddSerilog(Log.Logger);

        }

        private static void ConfigureConsole(LoggerConfiguration logging)
        {
            logging.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        private static void ConfigureAdminFile(LoggerConfiguration logging, string rootPath, int retention)
        {
            var fileName = $"{DateTime.Now:ddMMyyyy}.log";
            logging.WriteTo.File(
                path: Path.Combine(rootPath, fileName),
                rollingInterval: RollingInterval.Infinite,
                retainedFileCountLimit: retention);
        }

        private static void ConfigureMonitoringFile(LoggerConfiguration logging, string rootPath, int retention)
        {
            var fileName = $"{DateTime.Now:ddMMyyyy}.json";
            logging.WriteTo.File(
               formatter: new JsonFormatter(),
               path: Path.Combine(rootPath, fileName),
               rollingInterval: RollingInterval.Infinite,
               retainedFileCountLimit: retention
            );
        }
        private static LogEventLevel ParseLevel(string level)
        {
            return Enum.TryParse<LogEventLevel>(level, true, out var parsed)
                ? parsed
                : LogEventLevel.Information;
        }
    }
}
