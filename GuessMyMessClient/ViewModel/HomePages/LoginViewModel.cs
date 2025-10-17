using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.AuthService;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class LoginViewModel : ViewModelBase
    {
        // Propiedades enlazadas desde LoginView.xaml
        private string _usernameOrEmail;
        public string UsernameOrEmail { get => _usernameOrEmail; set { _usernameOrEmail = value; OnPropertyChanged(); } }

        private string _password; // Enlazada vía PasswordBoxHelper
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        public ICommand LoginCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand ReturnCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(UsernameOrEmail) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password.Length >= 6;
        }

        private async void ExecuteLogin(object parameter)
        {
            // Validaciones mínimas
            if (!CanExecuteLogin(parameter))
            {
                MessageBox.Show("Por favor, ingresa usuario/correo y contraseña.", "Error de Validación");
                return;
            }

            using (AuthenticationServiceClient client = new AuthenticationServiceClient())
            {
                try
                {
                    // Llamar al servicio de Login
                    // El servidor devuelve el Username en OperationResultDto.message si el login es exitoso.
                    OperationResultDto result = await client.LoginAsync(UsernameOrEmail, Password);

                    if (result.success)
                    {
                        // 1. Iniciar Sesión en el cliente
                        SessionManager.Instance.StartSession(result.message); // El mensaje es el Username

                        // 2. Navegar al Lobby
                        OpenLobby(parameter);
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error de Inicio de Sesión");
                    }
                }
                catch (EndpointNotFoundException)
                {
                    MessageBox.Show("Error de conexión: El servidor WCF no está disponible.", "Error Crítico");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error inesperado durante el login: {ex.Message}", "Error WCF");
                }
            }
        }

        private void OpenLobby(object parameter)
        {
            // Cerrar la ventana actual (LoginView)
            if (parameter is Window loginWindow)
            {
                loginWindow.Close();
            }

            // Abrir la ventana principal del Lobby
            var lobbyView = new LobbyView();
            lobbyView.Show();
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
        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.WindowState = currentWindow.WindowState;

                welcomeView.Show();
                currentWindow.Close();
            }
        }
    }
}
