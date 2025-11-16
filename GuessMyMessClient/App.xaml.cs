using GuessMyMessClient.ViewModel.Support.Navigation;
using GuessMyMessClient.ViewModel.Support;
using Serilog; 
using System.Globalization;
using System.Threading;
using System.Windows;

namespace GuessMyMessClient
{
    public partial class App : Application
    {
        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() 
                .WriteTo.File("logs/client_log_.txt", 
                    rollingInterval: RollingInterval.Day, 
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("--- Iniciando GuessMyMessClient ---");

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            base.OnStartup(e);
            ServiceLocator.Navigation = new WpfNavigationService();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "Error no controlado del cliente (Dispatcher).");
            e.Handled = true;

            Log.CloseAndFlush();
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("--- Cliente deteniéndose ---");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}