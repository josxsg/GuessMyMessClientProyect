using GuessMyMessClient.View.Matches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.View.Lobby; 
using GuessMyMessClient.ViewModel.Lobby;

namespace GuessMyMessClient.ViewModel.Matches
{
    public class PrivateMatchesViewModel : ViewModelBase
    {
        public ICommand PublicMatchesCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand ReturnCommand { get; }

        public PrivateMatchesViewModel()
        {
            PublicMatchesCommand = new RelayCommand(ExecutePublicMatches);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }
        private void ExecutePublicMatches(object obj)
        {
            PublicMatchesView publicMatchesView = new PublicMatchesView();
            PublicMatchesViewModel publicMatchesViewModel = new PublicMatchesViewModel();
            publicMatchesView.DataContext = publicMatchesViewModel;
            publicMatchesView.Show();

            Window privateMatchesWindow = Application.Current.Windows.OfType<PrivateMatchesView>().FirstOrDefault();
            if (privateMatchesWindow != null)
            {
                privateMatchesWindow.Close();
            }
            else
            {
                MessageBox.Show("Error: No se pudo encontrar la ventana de Partidas Privadas para cerrarla.", "Error");
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