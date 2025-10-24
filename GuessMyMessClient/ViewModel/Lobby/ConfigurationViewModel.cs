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
using GuessMyMessClient.ViewModel;
using System.ServiceModel;

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
            bool sessionClosedLocally = false;
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;

                if (!string.IsNullOrEmpty(currentUsername))
                {
                    using (var authClient = new AuthenticationServiceClient())
                    {
                        try
                        {
                            authClient.LogOut(currentUsername);
                        }
                        catch (CommunicationException commEx)
                        {
                            Console.WriteLine($"Error de comunicación al cerrar sesión en servidor: {commEx.Message}");
                        }
                    }
                }

                SessionManager.Instance.CloseSession();
                sessionClosedLocally = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado durante el cierre de sesión: {ex.Message}");
                if (!sessionClosedLocally)
                {
                    SessionManager.Instance.CloseSession();
                }
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
