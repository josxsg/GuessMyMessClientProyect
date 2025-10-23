using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.View.MatchSettings;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.Lobby;
using GuessMyMessClient.ViewModel.WaitingRoom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.MatchSettings
{
    public class PublicMatchSettingsViewModel : ViewModelBase
    {
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand CreateMatchCommand { get; }
        public ICommand ReturnCommand { get; }


        public PublicMatchSettingsViewModel() 
        {
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            CreateMatchCommand = new RelayCommand(ExecuteCreateMatch);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }

        private void ExecuteCreateMatch(object parameter)
        {
            // TODO: Aquí irá la lógica para llamar al servidor y crear la partida
            //       usando las propiedades como MatchName, Rounds, MaxPlayers, etc.
            //       Por ahora, simulamos la creación exitosa y navegamos.
            WaitingRoomPublicMatchHostViewModel waitingRoomViewModel = new WaitingRoomPublicMatchHostViewModel();
            WaitingRoomPublicMatchHostView waitingRoomView = new WaitingRoomPublicMatchHostView();
            waitingRoomView.DataContext = waitingRoomViewModel;
            waitingRoomView.Show();

            Window currentSettingsWindow = Application.Current.Windows.OfType<PublicMatchSettingsView>().FirstOrDefault();

            if (currentSettingsWindow != null)
            {
                currentSettingsWindow.Close();
            }
            else
            {
                MessageBox.Show("Error: No se pudo encontrar la ventana de configuración para cerrarla.", "Error");
            }
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window window)
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
            LobbyView lobbyView = new LobbyView();
            lobbyView.DataContext = new LobbyViewModel();
            lobbyView.Show();

            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }

    
}
