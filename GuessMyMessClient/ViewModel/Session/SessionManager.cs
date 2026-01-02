using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.View.HomePages;
using System.Windows;
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.Session
{
    public class SessionManager : ViewModelBase
    {
        private static SessionManager _instance;
        public static SessionManager Instance => _instance ?? (_instance = new SessionManager());

        private string _currentUsername;
        public string CurrentUsername
        {
            get
            {
                return _currentUsername;
            }
            set
            {
                _currentUsername = value; 
                OnPropertyChanged();
            }
        }

        private bool _isGuest;
        public bool IsGuest
        {
            get
            {
                return _isGuest;
            }
            set
            {
                _isGuest = value; 
                OnPropertyChanged();
            }
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);

        private SessionManager() { }

        public void StartSession(string username)
        {
            CurrentUsername = username;
        }

        public void CloseSession()
        {
            CurrentUsername = null;
            IsGuest = false; 
        }

        public void HandleServerDisconnect()
        {
            if (string.IsNullOrEmpty(CurrentUsername))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseSession();
                CleanupAllConnections();
                string message = Lang.serverConnectionLostMessage;
                string title = Lang.alertErrorTitle;
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigateToWelcomeView();
            });
        }

        private void CleanupAllConnections()
        {
            SocialClientManager.Instance.Cleanup();
            LobbyClientManager.Instance.Disconnect(); 
            GameClientManager.Instance.Disconnect();
            MatchmakingClientManager.Instance.Disconnect();
        }

        private void NavigateToWelcomeView()
        {
            WelcomeView welcomeView = new WelcomeView();
            welcomeView.Show();

            foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
            {
                if (window != welcomeView)
                {
                    window.Close();
                }
            }
        }
    }
}
