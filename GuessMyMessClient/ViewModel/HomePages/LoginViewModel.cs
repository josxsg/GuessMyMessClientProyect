using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class LoginViewModel : ViewModelBase
    {
        private string _usernameOrEmail;
        public string UsernameOrEmail
        {
            get
            {
                return _usernameOrEmail;
            }
            set
            {
                _usernameOrEmail = value; 
                OnPropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
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
            bool isSuccess = false;

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
                    isSuccess = true;
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
            catch (FaultException<ServiceFaultDto> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertLoginErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (FaultException)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private static void OpenLobby(object parameter)
        {
            if (parameter is Window loginWindow)
            {
                loginWindow.Close();
            }

            var lobbyView = new LobbyView();
            lobbyView.Show();
        }

        private static void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                Application.Current.Shutdown();
            }
        }

        private static void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private static void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private static void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.WindowState = currentWindow.WindowState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
                welcomeView.Show();
                currentWindow.Close();
            }
        }
    }
}
