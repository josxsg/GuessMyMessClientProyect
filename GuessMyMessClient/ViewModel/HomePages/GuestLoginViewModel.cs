using GuessMyMessClient.AuthService;
using GuessMyMessClient.ViewModel.Session;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.View.HomePages;
using System.Linq;
using System;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.WaitingRoom;
using GuessMyMessClient.Properties.Langs; 

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class GuestLoginViewModel : ViewModelBase
    {
        private string _email;
        private string _invitationCode;

        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }
        public string InvitationCode
        {
            get
            {
                return _invitationCode;
            }
            set
            {
                _invitationCode = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginGuestCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand BackCommand { get; }

        public GuestLoginViewModel()
        {
            LoginGuestCommand = new RelayCommand(ExecuteLoginGuest);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        private async void ExecuteLoginGuest(object obj)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(InvitationCode))
            {
                ShowAlert(Lang.alertRequiredFields, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            var client = new AuthenticationServiceClient();
            try
            {
                var result = await client.LoginAsGuestAsync(Email, InvitationCode);

                if (result.Success)
                {
                    string sessionToken = result.Message;
                    string matchId = result.Data["MatchId"];
                    bool isPrivate = bool.Parse(result.Data["IsPrivate"]);

                    SessionManager.Instance.StartSession(sessionToken);
                    SessionManager.Instance.IsGuest = true;

                    LobbyClientManager.Instance.Connect(sessionToken, matchId);

                    Window waitingRoomWindow;

                    if (isPrivate)
                    {
                        var vm = new WaitingRoomPrivateMatchViewModel(
                            LobbyClientManager.Instance,
                            SessionManager.Instance);
                        waitingRoomWindow = new WaitingRoomPrivateMatchView { DataContext = vm };
                    }
                    else
                    {
                        var vm = new WaitingRoomPublicMatchViewModel(
                            LobbyClientManager.Instance,
                            SessionManager.Instance);
                        waitingRoomWindow = new WaitingRoomPublicMatchView { DataContext = vm };
                    }

                    waitingRoomWindow.Show();

                    Application.Current.Windows.OfType<GuestLoginView>().FirstOrDefault()?.Close();
                }
                else
                {
                    ShowServiceError(result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                try { client.Close(); } catch { client.Abort(); }
            }
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

        private static void ExecuteBack(object obj)
        {
            new WelcomeView().Show();
            Application.Current.Windows.OfType<GuestLoginView>().FirstOrDefault()?.Close();
        }
    }
}