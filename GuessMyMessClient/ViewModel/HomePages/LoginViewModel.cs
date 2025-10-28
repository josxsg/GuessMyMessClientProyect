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
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class LoginViewModel : ViewModelBase
    {
        private string _usernameOrEmail;
        public string UsernameOrEmail
        {
            get => _usernameOrEmail;
            set
            {
                _usernameOrEmail = value;
                OnPropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

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
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async void ExecuteLogin(object parameter)
        {
            if (!CanExecuteLogin(parameter))
            {
                MessageBox.Show(
                    Lang.alertRequiredFields,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var client = new AuthenticationServiceClient();
            bool success = false;

            try
            {
                OperationResultDto result = await client.LoginAsync(UsernameOrEmail, Password);

                if (result.Success)
                {
                    SessionManager.Instance.StartSession(result.Message);
                    MatchmakingClientManager.Initialize();
                    MatchmakingClientManager.Instance.Connect(SessionManager.Instance.CurrentUsername);
                    OpenLobby(parameter);
                    client.Close();
                    success = true;
                }
                else
                {
                    MessageBox.Show(
                        result.Message,
                        Lang.alertLoginErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (FaultException<string> fex)
            {
                MessageBox.Show(
                    fex.Detail,
                    Lang.alertLoginErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Unknown Error: {ex.Message}");
            }
            finally
            {
                if (!success && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
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

        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.Show();
                currentWindow.Close();
            }
        }
    }
}
