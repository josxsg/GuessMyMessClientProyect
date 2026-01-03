using GuessMyMessClient.ViewModel.Support.Navigation;
using GuessMyMessClient.ViewModel.Support;
using Serilog; 
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows;
using GuessMyMessClient.ViewModel.Session;
using System;

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

            ServicePointManager.ServerCertificateValidationCallback =
         (sender, certificate, chain, sslPolicyErrors) =>
         {
             // En producción, solo acepta si no hay errores (SslPolicyErrors.None)
             if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
             {
                 return true;
             }
             return true;
         };
            base.OnStartup(e);
            ServiceLocator.Navigation = new WpfNavigationService();
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
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
            GameClientManager.Instance.PrepareForExit();
            try
            {
                if (LobbyClientManager.Instance.IsConnected)
                {
                    LobbyClientManager.Instance.Disconnect();
                }

                if (GameClientManager.Instance.IsConnected)
                {
                    GameClientManager.Instance.Disconnect();
                }

                MatchmakingClientManager.Instance.Disconnect();

                if (SocialClientManager.Instance.IsConnected)
                {
                    SocialClientManager.Instance.Cleanup();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la limpieza de servicios al cerrar la aplicación.");
            }
            finally
            {
                // El Logger se cierra aquí para asegurar que registre todo lo anterior
                Log.CloseAndFlush();
                base.OnExit(e);

                // Cierre forzoso de hilos secundarios si los hubiera
                Environment.Exit(0);
            }
        }
    }
}