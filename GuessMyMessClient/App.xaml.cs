using System.Globalization;
using System.Threading;
using System.Windows;

namespace GuessMyMessClient
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // --- CÓDIGO PARA FORZAR EL IDIOMA INGLÉS ---

            // Establece la cultura a "en-US" (Inglés de Estados Unidos)
            CultureInfo cultureInfo = new CultureInfo("es-MX");

            // Aplica la cultura al hilo principal de la aplicación.
            // Esto asegura que todos los recursos (como los textos de Lang.resx) se carguen en inglés.
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            // --- FIN DEL CÓDIGO DE IDIOMA ---

            base.OnStartup(e);
        }
    }
}
