using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.View.HomePages;
using System.Globalization;
using System.Threading;
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.HomePages
{
    // Clase auxiliar para llenar el ComboBox
    public class LanguageItem
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class MainViewModel : ViewModelBase
    {
        public ICommand StartGameCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        // --- Propiedades para el ComboBox ---
        public List<LanguageItem> Languages { get; }
        private LanguageItem _selectedLanguage;

        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                // Solo actuamos si la selección cambia realmente
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));

                    // Si se seleccionó algo, ejecutamos el cambio de idioma
                    if (value != null)
                    {
                        ExecuteChangeLanguage(value.Code);
                    }
                }
            }
        }

        public MainViewModel()
        {
            StartGameCommand = new RelayCommand(StartGame);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            // 1. Definir las opciones del ComboBox
            // (Usamos nombres nativos: "Español" siempre en español, "English" siempre en inglés)
            Languages = new List<LanguageItem>
            {
                new LanguageItem { Name = "Español", Code = "es-MX" },
                new LanguageItem { Name = "English", Code = "en-US" }
            };

            // 2. Inicializar la selección basada en el idioma actual del hilo
            string currentCode = Thread.CurrentThread.CurrentUICulture.Name;

            // Buscamos coincidencias (ej. "es-MX" o si empieza con "es")
            _selectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode)
                                ?? Languages.FirstOrDefault(l => currentCode.StartsWith("es") && l.Code == "es-MX")
                                ?? Languages.FirstOrDefault();
        }

        private void ExecuteChangeLanguage(string newCode)
        {
            try
            {
                // PROTECCIÓN: Si el idioma seleccionado es el que YA tiene la app, no hacemos nada.
                // Esto evita bucles infinitos al recargar la ventana.
                if (Thread.CurrentThread.CurrentUICulture.Name == newCode) return;

                // 1. Aplicar la nueva cultura
                CultureInfo newCulture = new CultureInfo(newCode);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
                Lang.Culture = newCulture;

                // 2. Reiniciar la ventana MainView para reflejar cambios
                var newWindow = new MainView();
                newWindow.Show();

                // 3. Cerrar la ventana vieja
                // Buscamos la ventana que invocó este ViewModel (la ventana actual)
                var currentWindow = Application.Current.Windows.OfType<MainView>().FirstOrDefault(w => w.DataContext == this);

                if (currentWindow != null)
                {
                    // Si esta era la ventana principal, actualizamos la referencia
                    if (Application.Current.MainWindow == currentWindow)
                    {
                        Application.Current.MainWindow = newWindow;
                    }
                    currentWindow.Close();
                }
                else
                {
                    // Fallback por si no encontramos la ventana por DataContext
                    var w = Application.Current.Windows.OfType<Window>().FirstOrDefault(win => win.IsActive);
                    if (w is MainView) w.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertChangeLanguageError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StartGame(object parameter)
        {
            var welcomeView = new WelcomeView();
            welcomeView.Show();

            if (parameter is Window mainWindow)
            {
                mainWindow.Close();
            }
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                Application.Current.Shutdown();
            }
        }

        private void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}