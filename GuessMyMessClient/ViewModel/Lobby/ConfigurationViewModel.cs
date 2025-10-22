using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class ConfigurationViewModel : ViewModelBase
    {
        public ICommand LogOutCommand { get; }

        public ConfigurationViewModel()
        {
            LogOutCommand = new RelayCommand(ExecuteLogout);
        }

        private void ExecuteLogout(object parameter)
        {
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;

                if (!string.IsNullOrEmpty(currentUsername))
                {

                    var authClient = new AuthenticationServiceClient();
                    authClient.LogOut(currentUsername);
                    authClient.Close();
                }

                SessionManager.Instance.CloseSession();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cerrar sesión en el servidor: " + ex.Message);
                SessionManager.Instance.CloseSession();
            }

            var mainView = new Main();
            mainView.Show();

            Window currentLobbyWindow = Application.Current.Windows.OfType<LobbyView>().FirstOrDefault();

            if (currentLobbyWindow != null)
            {
                currentLobbyWindow.Close();
            }
        }
    }
}
