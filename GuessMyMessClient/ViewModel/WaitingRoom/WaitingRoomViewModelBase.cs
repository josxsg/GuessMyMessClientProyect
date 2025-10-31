using GuessMyMessClient.LobbyService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;


namespace GuessMyMessClient.ViewModel.WaitingRoom
{
    public abstract class WaitingRoomViewModelBase : ViewModelBase
    {
        protected readonly LobbyClientManager _lobbyManager;
        protected readonly SessionManager _sessionManager;
        protected DispatcherTimer _countdownTimer;

        private string _matchName;
        public string MatchName
        {
            get => _matchName;
            set => SetProperty(ref _matchName, value);
        }

        private string _hostUsername;
        public string HostUsername
        {
            get => _hostUsername;
            set => SetProperty(ref _hostUsername, value);
        }

        private string _difficulty;
        public string Difficulty
        {
            get => _difficulty;
            set => SetProperty(ref _difficulty, value);
        }

        private string _playerCountDisplay;
        public string PlayerCountDisplay
        {
            get => _playerCountDisplay;
            set => SetProperty(ref _playerCountDisplay, value);
        }

        private string _matchCode;
        public string MatchCode
        {
            get => _matchCode;
            set => SetProperty(ref _matchCode, value);
        }

        private Visibility _matchCodeVisibility = Visibility.Collapsed;
        public Visibility MatchCodeVisibility
        {
            get => _matchCodeVisibility;
            set => SetProperty(ref _matchCodeVisibility, value);
        }

        public ObservableCollection<PlayerViewModel> Players { get; } = new ObservableCollection<PlayerViewModel>();
        public ObservableCollection<ChatMessageDisplay> ChatMessages { get; } = new ObservableCollection<ChatMessageDisplay>();

        private int _countdownValue;
        public int CountdownValue
        {
            get => _countdownValue;
            set => SetProperty(ref _countdownValue, value);
        }

        private Visibility _countdownVisibility = Visibility.Collapsed;
        public Visibility CountdownVisibility
        {
            get => _countdownVisibility;
            set => SetProperty(ref _countdownVisibility, value);
        }

        public bool IsHost => _sessionManager.CurrentUsername != null && _sessionManager.CurrentUsername.Equals(HostUsername, StringComparison.OrdinalIgnoreCase);

        public ICommand LeaveCommand { get; protected set; }
        public ICommand SendMessageCommand { get; protected set; }
        public ICommand KickPlayerCommand { get; protected set; }

        public WaitingRoomViewModelBase(LobbyClientManager lobbyManager, SessionManager sessionManager)
        {
            _lobbyManager = lobbyManager;
            _sessionManager = sessionManager;
            InitializeCommands();
            SubscribeToLobbyEvents();
        }

        protected virtual void InitializeCommands()
        {
            LeaveCommand = new RelayCommand(LeaveLobby);
        }

        protected virtual void SubscribeToLobbyEvents()
        {
            _lobbyManager.LobbyStateUpdated += OnLobbyStateUpdated;
            _lobbyManager.LobbyMessageReceived += OnLobbyMessageReceived;
            _lobbyManager.Kicked += OnKicked;
            _lobbyManager.CountdownTick += OnCountdownTick;
            _lobbyManager.GameStarted += OnGameStarted;
            _lobbyManager.ConnectionLost += OnConnectionLost;
        }

        protected virtual void UnsubscribeFromLobbyEvents()
        {
            _lobbyManager.LobbyStateUpdated -= OnLobbyStateUpdated;
            _lobbyManager.LobbyMessageReceived -= OnLobbyMessageReceived;
            _lobbyManager.Kicked -= OnKicked;
            _lobbyManager.CountdownTick -= OnCountdownTick;
            _lobbyManager.GameStarted -= OnGameStarted;
            _lobbyManager.ConnectionLost -= OnConnectionLost;
        }

        protected virtual void OnLobbyStateUpdated(LobbyStateDto state)
        {
            MatchName = state.MatchName;
            HostUsername = state.HostUsername;
            Difficulty = state.Difficulty;
            PlayerCountDisplay = $"{state.CurrentPlayers}/{state.MaxPlayers}";
            MatchCode = state.MatchCode;
            MatchCodeVisibility = string.IsNullOrEmpty(state.MatchCode) ? Visibility.Collapsed : Visibility.Visible;

            Players.Clear();
            bool amIHost = _sessionManager.CurrentUsername?.Equals(HostUsername, StringComparison.OrdinalIgnoreCase) ?? false;
            foreach (var username in state.PlayerUsernames.OrderBy(u => u))
            {
                var playerVM = new PlayerViewModel { Username = username };
                if (amIHost && !username.Equals(_sessionManager.CurrentUsername, StringComparison.OrdinalIgnoreCase))
                {
                    playerVM.KickButtonVisibility = Visibility.Visible;
                    playerVM.KickCommand = KickPlayerCommand;
                }
                Players.Add(playerVM);
            }
            OnPropertyChanged(nameof(IsHost));
        }

        protected virtual void OnLobbyMessageReceived(ChatMessageDto message)
        {
            string translatedMessage = TranslateMessageKey(message.MessageContent);
            string formatted = $"[{message.SenderUsername}]: {translatedMessage}";
            ChatMessages.Add(new ChatMessageDisplay { FormattedMessage = formatted });
            const int maxMessages = 100;
            if (ChatMessages.Count > maxMessages)
            {
                ChatMessages.RemoveAt(0);
            }
        }

        protected virtual void OnKicked(string reason)
        {
            MessageBox.Show($"{Lang.waitingRoomMsgKicked}: {reason}", Lang.alertInfoTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            NavigateBackToLobbyView();
            CleanUp();
        }

        protected virtual void OnCountdownTick(int secondsRemaining)
        {
            CountdownValue = secondsRemaining;
            CountdownVisibility = Visibility.Visible;
        }

        protected virtual void OnGameStarted()
        {
            CountdownVisibility = Visibility.Collapsed;
            MessageBox.Show(Lang.waitingRoomMsgGameStarting, Lang.alertInfoTitle);
            CleanUp();
        }

        protected virtual void OnConnectionLost()
        {
            NavigateBackToLobbyView();
            CleanUp();
        }

        protected virtual void LeaveLobby(object parameter = null)
        {
            _lobbyManager.Disconnect();
            NavigateBackToLobbyView();
            CleanUp();
        }

        protected virtual void SendChatMessage(string messageKey)
        {
            if (!string.IsNullOrEmpty(messageKey))
            {
                _lobbyManager.SendChatMessage(messageKey);
            }
        }

        protected virtual void KickPlayer(string usernameToKick)
        {
            if (!string.IsNullOrEmpty(usernameToKick) && IsHost)
            {
                var result = MessageBox.Show(string.Format(Lang.waitingRoomMsgConfirmKick, usernameToKick),
                                             Lang.alertConfirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _lobbyManager.RequestKickPlayer(usernameToKick);
                }
            }
        }

        protected string TranslateMessageKey(string key)
        {
            var resource = Lang.ResourceManager.GetString(key);
            return string.IsNullOrEmpty(resource) ? key : resource;
        }

        protected virtual void NavigateBackToLobbyView()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window currentWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this);
                currentWindow?.Close();
                Console.WriteLine("Navigating back to Lobby (Implement actual navigation)");
            });
        }

        public virtual void CleanUp()
        {
            UnsubscribeFromLobbyEvents();
            _countdownTimer?.Stop();
            _countdownTimer = null;
            Console.WriteLine("WaitingRoom ViewModel cleaned up.");
        }
    }
    public class PlayerViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private Visibility _kickButtonVisibility = Visibility.Collapsed;
        public Visibility KickButtonVisibility
        {
            get => _kickButtonVisibility;
            set => SetProperty(ref _kickButtonVisibility, value);
        }

        public ICommand KickCommand { get; set; }
    }
}
