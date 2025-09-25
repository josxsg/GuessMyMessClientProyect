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

        public MainViewModel()
        {
            // Inicializamos el comando y le decimos qué método debe ejecutar
            StartGameCommand = new RelayCommand(StartGame);
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
    }
}
