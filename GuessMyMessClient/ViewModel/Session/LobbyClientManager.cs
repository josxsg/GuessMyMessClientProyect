using GuessMyMessClient.LobbyService;
using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.Properties.Langs;
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
        public string CurrentMatchId { get; private set; }
        private string _currentUsername;

        public LobbySettingsDto CurrentLobbySettings { get; private set; }

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
                if (IsConnected)
                {
                    Disconnect();
                }

                _currentUsername = username;
                CurrentMatchId = matchId;

                var instanceContext = new InstanceContext(this);
                _client = new LobbyServiceClient(instanceContext);
                _client.Open();

                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;

                _client.ConnectToLobby(username, matchId);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                CleanupConnection();
                ConnectionLost?.Invoke();
            }
        }

        public void Disconnect()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                if (_client.State == CommunicationState.Opened && !string.IsNullOrEmpty(_currentUsername))
                {
                    _client.LeaveLobby(_currentUsername, CurrentMatchId);
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                catch (Exception)
                {
                    MessageBox.Show(
                        Lang.alertUnknownErrorMessage,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                     _client.Abort();
                }
                finally
                {
                    _client = null;
                    CurrentMatchId = null;
                    _currentUsername = null;
                }
            }
        }

        public void SendChatMessage(string messageKey)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.SendLobbyMessage(_currentUsername, CurrentMatchId, messageKey);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                HandleCommunicationError();
            }
        }

        public void RequestStartGame()
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.StartGame(_currentUsername, CurrentMatchId);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error); 
                HandleCommunicationError();
            }
        }

        public void RequestKickPlayer(string playerToKick)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.KickPlayer(_currentUsername, playerToKick, CurrentMatchId);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                HandleCommunicationError();
            }
        }

        public void SetCurrentLobbySettings(LobbySettingsDto settings)
        {
            CurrentLobbySettings = settings;
        }

        public void UpdateLobbyState(LobbyStateDto lobbyStateDto)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LobbyStateUpdated?.Invoke(lobbyStateDto);
            });
        }

        public void ReceiveLobbyMessage(ChatMessageDto messageDto)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LobbyMessageReceived?.Invoke(messageDto);
            });
        }

        public void KickedFromLobby(string reason)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Kicked?.Invoke(reason);
                CleanupConnection();
            });
        }

        public void OnGameStarting(int countdownSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CountdownTick?.Invoke(countdownSeconds);
            });
        }

        public void OnGameStarted()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                GameStarted?.Invoke();
            });
        }

        public void UpdateKickVote(string targetUsername, int currentVotes, int votesNeeded)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Vote update: {targetUsername} {currentVotes}/{votesNeeded}");
            });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            HandleCommunicationError(true);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            if (_client != null)
            {
                HandleCommunicationError(true);
            }
        }

        private void HandleCommunicationError(bool unexpected = false)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CleanupConnection();
                ConnectionLost?.Invoke();

                if (unexpected)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertConnectionErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            });
        }
    }

    public class ChatMessageDisplay
    {
        public string FormattedMessage { get; set; }
    }
}
