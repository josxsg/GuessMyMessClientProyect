using GuessMyMessClient.GameService;
using GuessMyMessClient.LobbyService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private int _isNavigatingBack = 0;
        private string _matchName;
        public string MatchName
        {
            get
            {
                return _matchName;
            }
            set
            {
                SetProperty(ref _matchName, value);
            }
        }

        private string _hostUsername;
        public string HostUsername
        {
            get
            {
                return _hostUsername;
            }
            set
            {
                SetProperty(ref _hostUsername, value);
            }
        }

        private string _difficulty;
        public string Difficulty
        {
            get
            {
                return _difficulty;
            }
            set
            {
                SetProperty(ref _difficulty, value);
            }
        }

        private string _playerCountDisplay;
        public string PlayerCountDisplay
        {
            get
            {
                return _playerCountDisplay;
            }
            set
            {
                SetProperty(ref _playerCountDisplay, value);
            }
        }

        private string _matchCode;
        public string MatchCode
        {
            get
            {
                return _matchCode;
            }
            set
            {
                SetProperty(ref _matchCode, value);
            }
        }

        private Visibility _matchCodeVisibility = Visibility.Collapsed;
        public Visibility MatchCodeVisibility
        {
            get
            {
                return _matchCodeVisibility;
            }
            set
            {
                SetProperty(ref _matchCodeVisibility, value);
            }
        }

        public ObservableCollection<PlayerViewModel> Players { get; } = new ObservableCollection<PlayerViewModel>();
        public ObservableCollection<ChatMessageDisplay> ChatMessages { get; } = new ObservableCollection<ChatMessageDisplay>();

        private int _countdownValue;
        public int CountdownValue
        {
            get
            {
                return _countdownValue;
            }
            set
            {
                SetProperty(ref _countdownValue, value);
            }
        }

        private Visibility _countdownVisibility = Visibility.Collapsed;
        public Visibility CountdownVisibility
        {
            get
            {
                return _countdownVisibility;
            }
            set
            {
                SetProperty(ref _countdownVisibility, value);
            }
        }

        public bool IsHost => _sessionManager.CurrentUsername != null &&
                              _sessionManager.CurrentUsername.Equals(HostUsername, StringComparison.OrdinalIgnoreCase);

        public ICommand LeaveCommand { get; protected set; }
        public ICommand SendMessageCommand { get; protected set; }
        public ICommand KickPlayerCommand { get; protected set; }
        public ICommand InviteCommand { get; protected set; }

        protected WaitingRoomViewModelBase(LobbyClientManager lobbyManager, SessionManager sessionManager)
        {
            _lobbyManager = lobbyManager;
            _sessionManager = sessionManager;
            InitializeCommands();
            SubscribeToLobbyEvents();
        }

        protected virtual void InitializeCommands()
        {
            LeaveCommand = new RelayCommand(LeaveLobby);
            SendMessageCommand = new RelayCommand((param) => SendChatMessage(param as string));
            KickPlayerCommand = new RelayCommand((param) => KickPlayer(param as string));
            InviteCommand = new RelayCommand(ExecuteInvite);
        }

        protected virtual void SubscribeToLobbyEvents()
        {
            _lobbyManager.LobbyStateUpdated += OnLobbyStateUpdated;
            _lobbyManager.LobbyMessageReceived += OnLobbyMessageReceived;
            _lobbyManager.Kicked += OnKicked;
            _lobbyManager.CountdownTick += OnCountdownTick;
            _lobbyManager.GameStarted += OnGameStarted;
            _lobbyManager.ConnectionLost += OnConnectionLost;
            GameClientManager.Instance.RoundStart += OnRoundStartFromGame;
        }

        protected virtual void OnLobbyStateUpdated(LobbyStateDto state)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MatchName = state.MatchName;
                HostUsername = state.HostUsername;
                Difficulty = state.Difficulty;
                PlayerCountDisplay = $"{state.CurrentPlayers}/{state.MaxPlayers}";
                MatchCode = state.MatchCode;
                MatchCodeVisibility = string.IsNullOrEmpty(state.MatchCode) ? Visibility.Collapsed : Visibility.Visible;

                Players.Clear();
                bool amIHost = IsHost;

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
                if (!GameClientManager.Instance.IsConnected)
                {
                    string myUser = _sessionManager.CurrentUsername;
                    string matchId = _lobbyManager.CurrentMatchId;
                    if (!string.IsNullOrEmpty(myUser) && !string.IsNullOrEmpty(matchId))
                    {
                        GameClientManager.Instance.Connect(myUser, matchId);
                    }
                }
            });
        }

        protected virtual void OnLobbyMessageReceived(ChatMessageDto message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_sessionManager.IsGuest &&
                    message.SenderUsername == "System" &&
                    !string.IsNullOrEmpty(message.MessageContent) &&
                    message.MessageContent.StartsWith(Lang.infoGuestName))
                {
                    string assignedName = message.MessageContent.Substring(Lang.infoGuestName.Length).Trim();

                    _sessionManager.CurrentUsername = assignedName;
                    OnPropertyChanged(nameof(IsHost));
                }

                string translatedMessage = TranslateMessageKey(message.MessageContent);
                string formatted = $"[{message.SenderUsername}]: {translatedMessage}";

                ChatMessages.Add(new ChatMessageDisplay { FormattedMessage = formatted });

                const int MaxMessages = 100;
                if (ChatMessages.Count > MaxMessages)
                {
                    ChatMessages.RemoveAt(0);
                }
            });
        }

        protected virtual void OnKicked(string reason)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isNavigatingBack, 1, 0) != 0)
            {
                return;
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"{Lang.waitingRoomMsgKicked}: {reason}", 
                    Lang.alertInfoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NavigateBackToLobbyView();
                CleanUp();
            });
        }

        protected virtual void OnCountdownTick(int secondsRemaining)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CountdownValue = secondsRemaining;
                CountdownVisibility = Visibility.Visible;
            });
        }

        private void OnRoundStartFromGame(object sender, RoundStartEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GameClientManager.Instance.RoundStart -= OnRoundStartFromGame;
                CleanUp();
                Window myWindow = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                ServiceLocator.Navigation.NavigateToWordSelection();
                if (myWindow != null)
                {
                    myWindow.Close();
                }
            });
        }

        protected virtual void OnGameStarted()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isNavigatingBack, 1, 0) != 0)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CountdownVisibility = Visibility.Collapsed;

                if (IsHost)
                {
                    try
                    {
                        int rounds = _lobbyManager.CurrentLobbySettings?.TotalRounds ?? 3;
                        var playerList = Players.Select(p => p.Username).ToList();
                        GameClientManager.Instance.StartGame(rounds, playerList);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            Lang.alertGameStartError, 
                            Lang.alertErrorTitle, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                }
            });
        }

        protected virtual void OnConnectionLost()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isNavigatingBack, 1, 0) != 0)
            {
                return;
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                NavigateBackToLobbyView();
                CleanUp();
            });
        }

        protected virtual void LeaveLobby(object parameter = null)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isNavigatingBack, 1, 0) != 0)
            {
                return;
            }

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
                var result = MessageBox.Show(
                    string.Format(Lang.waitingRoomMsgConfirmKick, usernameToKick),
                    Lang.alertConfirmationTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _lobbyManager.RequestKickPlayer(usernameToKick);
                }
            }
        }

        private void ExecuteInvite(object obj)
        {
            string currentMatchId = _lobbyManager.CurrentMatchId;
            if (string.IsNullOrEmpty(currentMatchId))
            {
                return;
            }

            var inviteVM = new InviteByEmailViewModel(currentMatchId);
            var inviteView = new InviteByEmailView
            {
                DataContext = inviteVM,
                Owner = Application.Current.MainWindow
            };
            inviteView.ShowDialog();
        }

        protected static string TranslateMessageKey(string key)
        {
            var resource = Lang.ResourceManager.GetString(key);
            return string.IsNullOrEmpty(resource) ? key : resource;
        }

        protected virtual void NavigateBackToLobbyView()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window currentWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this);

                if (_sessionManager.IsGuest)
                {
                    _sessionManager.CloseSession();
                    var mainView = new MainView();
                    mainView.Show();
                }
                else
                {
                    var lobbyView = new LobbyView();
                    lobbyView.Show();
                }

                currentWindow?.Close();
            });
        }

        public virtual void CleanUp()
        {
            UnsubscribeFromLobbyEvents();
            _countdownTimer?.Stop();
            _countdownTimer = null;
        }

        protected virtual void UnsubscribeFromLobbyEvents()
        {
            _lobbyManager.LobbyStateUpdated -= OnLobbyStateUpdated;
            _lobbyManager.LobbyMessageReceived -= OnLobbyMessageReceived;
            _lobbyManager.Kicked -= OnKicked;
            _lobbyManager.CountdownTick -= OnCountdownTick;
            _lobbyManager.GameStarted -= OnGameStarted;
            _lobbyManager.ConnectionLost -= OnConnectionLost;
            GameClientManager.Instance.RoundStart -= OnRoundStartFromGame;
        }
    }

    public class PlayerViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                SetProperty(ref _username, value);
            }
        }

        private Visibility _kickButtonVisibility = Visibility.Collapsed;
        public Visibility KickButtonVisibility
        {
            get
            {
                return _kickButtonVisibility;
            }
            set
            {
                SetProperty(ref _kickButtonVisibility, value);
            }
        }

        public ICommand KickCommand { get; set; }
    }
}
