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
            return IsHost && Players != null && Players.Count >= 2;
        }

        private void StartGame(object parameter)
        {
            _lobbyManager.RequestStartGame();
        }

        protected override void OnLobbyStateUpdated(LobbyStateDto state)
        {
            base.OnLobbyStateUpdated(state);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();
            });
        }
    }
}