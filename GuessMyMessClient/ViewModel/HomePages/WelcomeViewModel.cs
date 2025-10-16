using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;

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

        public WelcomeViewModel()
        {
            SignUpCommand = new RelayCommand(SignUp);
            LoginCommand = new RelayCommand(Login);
            ContinueAsGuestCommand = new RelayCommand(ContinueAsGuest);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
        }

        private void SignUp(object parameter)
        {
            if (parameter is Window welcomeWindow)
            {
                var signUpView = new SignUpView();

                // 1. Hereda el estado de la ventana ANTES de mostrarla
                signUpView.WindowState = welcomeWindow.WindowState;

                signUpView.Show();
                welcomeWindow.Close();
            }
        }

        private void Login(object parameter)
        {
            if (parameter is Window welcomeWindow)
            {
                var loginView = new LoginView();

                // 1. Hereda el estado de la ventana ANTES de mostrarla
                loginView.WindowState = welcomeWindow.WindowState;

                loginView.Show();
                welcomeWindow.Close();
            }
        }

        private void ContinueAsGuest(object parameter)
        {
            var lobbyView = new LobbyView();
            lobbyView.Show();

            if (parameter is Window welcomeWindow)
            {
                welcomeWindow.Close();
            }
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
    }
}
