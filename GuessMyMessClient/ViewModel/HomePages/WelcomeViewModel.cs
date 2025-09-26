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

        public WelcomeViewModel()
        {
            SignUpCommand = new RelayCommand(SignUp);
            LoginCommand = new RelayCommand(Login);
            ContinueAsGuestCommand = new RelayCommand(ContinueAsGuest);
        }

        private void SignUp(object parameter)
        {
            var signUpView = new SignUpView();
            signUpView.Show();

            if (parameter is Window welcomeWindow)
            {
                welcomeWindow.Close();
            }
        }

        private void Login(object parameter)
        {
            var loginView = new LoginView();
            loginView.Show();

            if (parameter is Window welcomeWindow)
            {
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
    }
}
