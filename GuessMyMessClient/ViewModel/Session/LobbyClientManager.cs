using GuessMyMessClient.LobbyService;
using System;
using System.ServiceModel;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Session
{
    public class LobbyClientManager : ILobbyServiceCallback
    {
        private static readonly Lazy<LobbyClientManager> _lazyInstance =
            new Lazy<LobbyClientManager>(() => new LobbyClientManager());

        public static LobbyClientManager Instance => _lazyInstance.Value;

        private LobbyClientManager() { }

        private LobbyServiceClient _client;
        private InstanceContext _instanceContext;
        private string _currentMatchId;
        private string _currentUsername;

        public event Action<LobbyStateDto> LobbyStateUpdated;
        public event Action<ChatMessageDto> LobbyMessageReceived;
        public event Action<string> Kicked;
        public event Action<int> CountdownTick;
        public event Action GameStarted;
        public event Action ConnectionLost;

        public bool IsConnected => _client != null && _client.State == CommunicationState.Opened;

        public void Connect(string username, string matchId)
        {
            try
            {
                if (IsConnected) Disconnect();
                _currentUsername = username;
                _currentMatchId = matchId;
                _instanceContext = new InstanceContext(this);
                _client = new LobbyServiceClient(_instanceContext);
                _client.Open();
                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;
                _client.ConnectToLobby(username, matchId);
                Console.WriteLine($"Attempting to connect to lobby {matchId} as {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Lobby service: {ex.Message}");
                MessageBox.Show($"Error connecting to lobby: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CleanupConnection();
                ConnectionLost?.Invoke();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            try
            {
                _client.LeaveLobby(_currentUsername, _currentMatchId);
                Console.WriteLine($"Sent LeaveLobby request for {_currentUsername} from {_currentMatchId}");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Console.WriteLine($"Failed to send LeaveLobby gracefully: {ex.Message}. Forcing disconnect.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during LeaveLobby: {ex.Message}");
            }
            finally
            {
                CleanupConnection();
            }
        }

        private void CleanupConnection()
        {
            if (_client != null)
            {
                try
                {
                    _client.InnerChannel.Faulted -= Channel_Faulted;
                    _client.InnerChannel.Closed -= Channel_Closed;
                }
                catch { }
                try
                {
                    if (_client.State != CommunicationState.Faulted)
                    {
                        _client.Close();
                    }
                    else
                    {
                        _client.Abort();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during WCF proxy cleanup: {ex.Message}");
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                    _instanceContext = null;
                    _currentMatchId = null;
                    _currentUsername = null;
                }
            }
            Console.WriteLine("WCF connection cleaned up.");
        }

        public void SendChatMessage(string messageKey)
        {
            if (!IsConnected || string.IsNullOrEmpty(_currentUsername) || string.IsNullOrEmpty(_currentMatchId)) return;
            try
            {
                _client.SendLobbyMessage(_currentUsername, _currentMatchId, messageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending chat message: {ex.Message}");
                HandleCommunicationError();
            }
        }

        public void RequestStartGame()
        {
            if (!IsConnected || string.IsNullOrEmpty(_currentUsername) || string.IsNullOrEmpty(_currentMatchId)) return;
            try
            {
                _client.StartGame(_currentUsername, _currentMatchId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting start game: {ex.Message}");
                HandleCommunicationError();
            }
        }

        public void RequestKickPlayer(string playerToKick)
        {
            if (!IsConnected || string.IsNullOrEmpty(_currentUsername) || string.IsNullOrEmpty(_currentMatchId) || string.IsNullOrEmpty(playerToKick)) return;
            try
            {
                _client.KickPlayer(_currentUsername, playerToKick, _currentMatchId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error kicking player {playerToKick}: {ex.Message}");
                HandleCommunicationError();
            }
        }

        public void UpdateLobbyState(LobbyStateDto lobbyStateDto)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Received Lobby State Update: {lobbyStateDto.CurrentPlayers}/{lobbyStateDto.MaxPlayers} players.");
                LobbyStateUpdated?.Invoke(lobbyStateDto);
            });
        }

        public void ReceiveLobbyMessage(ChatMessageDto messageDto)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Received Chat: [{messageDto.SenderUsername}]: {messageDto.MessageContent}");
                LobbyMessageReceived?.Invoke(messageDto);
            });
        }

        public void KickedFromLobby(string reason)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Kicked from lobby: {reason}");
                Kicked?.Invoke(reason);
                CleanupConnection();
            });
        }

        public void OnGameStarting(int countdownSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Game starting in {countdownSeconds}...");
                CountdownTick?.Invoke(countdownSeconds);
            });
        }

        public void OnGameStarted()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Game started!");
                GameStarted?.Invoke();
            });
        }

        public void UpdateKickVote(string targetUsername, int currentVotes, int votesNeeded)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Kick vote update for {targetUsername}: {currentVotes}/{votesNeeded}");
            });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("WCF channel has faulted.");
            HandleCommunicationError(true);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("WCF channel was closed.");
            if (_client != null)
            {
                HandleCommunicationError(true);
            }
        }

        private void HandleCommunicationError(bool unexpected = false)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Handling communication error.");
                CleanupConnection();
                ConnectionLost?.Invoke();
                if (unexpected)
                {
                    MessageBox.Show("Lost connection to the lobby service.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }
    }
    public class ChatMessageDisplay
    {
        public string FormattedMessage { get; set; }
    }
}
