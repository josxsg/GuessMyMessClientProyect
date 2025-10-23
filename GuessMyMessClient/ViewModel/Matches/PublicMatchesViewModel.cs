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
    public class PublicMatchesViewModel : ViewModelBase
    {
        public ICommand MinimizeWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand PrivateMatchesCommand { get; }
        public ICommand ReturnCommand { get; }

        public PublicMatchesViewModel()
        {
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            PrivateMatchesCommand = new RelayCommand(ExecutePrivateMatches);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }

        private void ExecutePrivateMatches(object obj)
        {
            PrivateMatchesView privateMatchesView = new PrivateMatchesView();
            PrivateMatchesViewModel privateMatchesViewModel = new PrivateMatchesViewModel();

            privateMatchesView.DataContext = privateMatchesViewModel;
            privateMatchesView.Show();

            Window publicMatchesWindow = Application.Current.Windows.OfType<PublicMatchesView>().FirstOrDefault();
            if (publicMatchesWindow != null)
            {
                publicMatchesWindow.Close();
            }
            else
            {
                MessageBox.Show("Error: No se pudo encontrar la ventana de Partidas Públicas para cerrarla.", "Error");
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