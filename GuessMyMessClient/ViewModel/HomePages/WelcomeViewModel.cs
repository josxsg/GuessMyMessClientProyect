using System;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.View.Lobby;
using System.Collections.Generic; // Para List
using System.Linq;                // Para FirstOrDefault
using System.Threading;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class WelcomeViewModel : ViewModelBase
    {
        public ICommand SignUpCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand ContinueAsGuestCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public List<LanguageItem> Languages { get; }
        private LanguageItem _selectedLanguage;

        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    if (value != null)
                    {
                        ChangeLanguageAndRestart<WelcomeView>(value.Code);
                    }
                }
            }
        }

        public WelcomeViewModel()
        {
            SignUpCommand = new RelayCommand(SignUp);
            LoginCommand = new RelayCommand(Login);
            ContinueAsGuestCommand = new RelayCommand(ContinueAsGuest);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            Languages = new List<LanguageItem>
            {
                new LanguageItem { Name = "Español", Code = "es-MX" },
                new LanguageItem { Name = "English", Code = "en-US" }
            };

            string currentCode = Thread.CurrentThread.CurrentUICulture.Name;
            _selectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode)
                                ?? Languages.FirstOrDefault(l => currentCode.StartsWith("es") && l.Code == "es-MX")
                                ?? Languages.FirstOrDefault();
        
        }

        private static void SignUp(object parameter)
        {
            if (parameter is Window welcomeWindow)
            {
                var signUpView = new SignUpView();
                signUpView.WindowState = welcomeWindow.WindowState;
                signUpView.WindowState = WindowState.Maximized;
                signUpView.WindowStyle = WindowStyle.None;
                signUpView.ResizeMode = ResizeMode.NoResize;
                signUpView.Show();
                welcomeWindow.Close();
            }
        }

        private static void Login(object parameter)
        {
            if (parameter is Window welcomeWindow)
            {
                var loginView = new LoginView();
                loginView.WindowState = welcomeWindow.WindowState;
                loginView.WindowState = WindowState.Maximized;
                loginView.WindowStyle = WindowStyle.None;
                loginView.ResizeMode = ResizeMode.NoResize;
                loginView.Show();
                welcomeWindow.Close();
            }
        }

        private static void ContinueAsGuest(object parameter)
        {
            if (parameter is Window welcomeWindow)
            {
                var guestLoginView = new GuestLoginView();
                guestLoginView.WindowState = welcomeWindow.WindowState;
                guestLoginView.WindowState = WindowState.Maximized;
                guestLoginView.WindowStyle = WindowStyle.None;
                guestLoginView.ResizeMode = ResizeMode.NoResize;
                guestLoginView.Show();
                welcomeWindow.Close();
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
    }
}
