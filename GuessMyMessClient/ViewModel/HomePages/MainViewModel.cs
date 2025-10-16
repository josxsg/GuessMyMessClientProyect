using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.View.HomePages;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class MainViewModel : ViewModelBase
    {
        // Comando que se enlazará al botón "COMENZAR"
        public ICommand StartGameCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public MainViewModel()
        {
            // Inicializamos el comando y le decimos qué método debe ejecutar
            StartGameCommand = new RelayCommand(StartGame);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
        }

        private void StartGame(object parameter)
        {
            // Crear y mostrar la nueva ventana de bienvenida
            var welcomeView = new WelcomeView();
            welcomeView.Show();

            // Cierra la ventana actual (Main.xaml)
            if (parameter is Window mainWindow)
            {
                mainWindow.Close();
            }
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                // Para la ventana principal, cerramos la aplicación
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
