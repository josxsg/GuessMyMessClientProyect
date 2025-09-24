using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

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
            // --- LÓGICA DE INICIO ---
            // Aquí es donde pondrás la lógica para lo que sucede al hacer clic.
            // Por ejemplo, navegar a la siguiente ventana de inicio de sesión o al lobby.

            // Por ahora, solo mostraremos un mensaje para verificar que funciona.
            MessageBox.Show("¡El juego ha comenzado! (Lógica del ViewModel)");

            // Ejemplo de cómo podrías abrir otra ventana y cerrar la actual:
            /*
            var loginView = new LoginView();
            loginView.Show();
            
            // Cierra la ventana actual (Main.xaml)
            if (parameter is Window window)
            {
                window.Close();
            }
            */
        }
    }
}
