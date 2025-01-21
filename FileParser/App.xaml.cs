using Serilog;
using System.Windows;

namespace FileParser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Настройка Serilog
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

            Log.Information("Приложение запущено");

            try
            {
                Log.Debug("Настройка завершена успешно.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Приложение завершилось с критической ошибкой.");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Завершение работы логгера
            Log.Information("Приложение завершено.");
            Log.CloseAndFlush();
        }
    }
}
