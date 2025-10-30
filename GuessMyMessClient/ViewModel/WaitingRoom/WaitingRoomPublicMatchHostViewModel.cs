using GuessMyMessClient.LobbyService;
using GuessMyMessClient.ViewModel.Session;
using System.Windows.Input;
using System.Windows;

namespace GuessMyMessClient.ViewModel.WaitingRoom
{
    public class WaitingRoomPublicMatchHostViewModel : WaitingRoomViewModelBase
    {
        public ICommand StartGameCommand { get; private set; }

        public WaitingRoomPublicMatchHostViewModel(LobbyClientManager lobbyManager, SessionManager sessionManager)
            : base(lobbyManager, sessionManager)
        {
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            StartGameCommand = new RelayCommand(StartGame, CanStartGame);
        }

        private bool CanStartGame(object parameter)
        {
            return IsHost;
        }

        private void StartGame(object parameter)
        {
            _lobbyManager.RequestStartGame();
        }

        protected override void OnLobbyStateUpdated(LobbyStateDto state)
        {
            bool wasHostBeforeUpdate = this.IsHost;

            base.OnLobbyStateUpdated(state); 

            if (wasHostBeforeUpdate != this.IsHost)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }
    }
}