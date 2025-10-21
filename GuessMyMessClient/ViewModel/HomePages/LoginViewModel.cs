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
        private string _usernameOrEmail;
        public string UsernameOrEmail { get => _usernameOrEmail; set { _usernameOrEmail = value; OnPropertyChanged(); } }

        private string _password; 
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
            if (!CanExecuteLogin(parameter))
            {
                MessageBox.Show("Por favor, ingresa usuario/correo y contraseña.", "Error de Validación");
                return;
            }

            var client = new AuthenticationServiceClient();

            try
            {
                OperationResultDto result = await client.LoginAsync(UsernameOrEmail, Password);

                if (result.success)
                {
                    SessionManager.Instance.StartSession(result.message);
                    OpenLobby(parameter);

                    client.Close();
                }
                else
                {
                    MessageBox.Show(result.message, "Error de Inicio de Sesión");
                    client.Abort();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar con el servidor. \nError: {ex.Message}", "Error de Conexión");
                client.Abort();
            }
        }

        private void OpenLobby(object parameter)
        {
            if (parameter is Window loginWindow)
            {
                loginWindow.Close();
            }

            var lobbyView = new LobbyView();
            lobbyView.Show();
        }
        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window window)
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
