using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessMyMessClient.ViewModel.Session
{
    public class SessionManager : ViewModelBase
    {
        private static SessionManager _instance;
        public static SessionManager Instance => _instance ?? (_instance = new SessionManager());

        private string _currentUsername;
        public string CurrentUsername
        {
            get => _currentUsername;
            set { _currentUsername = value; OnPropertyChanged(); }
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
        }
    }
}
