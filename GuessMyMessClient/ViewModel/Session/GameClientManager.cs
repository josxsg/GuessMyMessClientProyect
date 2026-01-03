using GuessMyMessClient.GameService;
using GuessMyMessClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

using ServiceGameFault = GuessMyMessClient.GameService.ServiceFaultDto;

namespace GuessMyMessClient.ViewModel.Session
{
    public class GameClientManager : IGameServiceCallback
    {
        private static readonly Lazy<GameClientManager> _lazyInstance =
            new Lazy<GameClientManager>(() => new GameClientManager());

        public static GameClientManager Instance => _lazyInstance.Value;

        private GameClientManager() { }

        private GameServiceClient _client;
        private string _currentUsername;
        private string _currentMatchId;
        private bool _isExiting = false;
        private const string EndpointName = "NetTcpBinding_IGameService";
        public string GetCurrentUsername()
        {
            return _currentUsername;
        }
        public bool IsConnected => _client != null && _client.State == CommunicationState.Opened;

        public event EventHandler<RoundStartEventArgs> RoundStart;
        public event EventHandler<DrawingPhaseStartEventArgs> DrawingPhaseStart;
        public event EventHandler<GuessingPhaseStartEventArgs> GuessingPhaseStart;
        public event EventHandler<GameEndEventArgs> GameEnd;
        public event Action ConnectionLost;
        public event EventHandler<InGameMessageEventArgs> InGameMessageReceived;
        public event EventHandler<AnswersPhaseStartEventArgs> AnswersPhaseStart;
        public event EventHandler<ShowNextDrawingEventArgs> ShowNextDrawing;

        public void PrepareForExit()
        {
            _isExiting = true;
            RoundStart = null;
            DrawingPhaseStart = null;
            GuessingPhaseStart = null;
            GameEnd = null;
            ConnectionLost = null;
            InGameMessageReceived = null;
            AnswersPhaseStart = null;
            ShowNextDrawing = null;
        }

        public void Connect(string username, string matchId)
        {
            _isExiting = false;
            try
            {
                if (IsConnected)
                {
                    Disconnect();
                }

                _currentUsername = username;
                _currentMatchId = matchId;

                var instanceContext = new InstanceContext(this);
                _client = new GameServiceClient(instanceContext, EndpointName);
                _client.Open();

                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;

                _client.Connect(_currentUsername, _currentMatchId);
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CleanupConnection();
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
                if (_client.State == CommunicationState.Opened)
                {
                    _client.Disconnect(_currentUsername, _currentMatchId);
                }
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
                }
            }
        }

        public async Task<WordDto[]> GetRandomWordsAsync(string username)
        {
            if (!IsConnected)
            {
                return null;
            }

            try
            {
                return await _client.GetRandomWordsAsync(username);
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message, 
                    Lang.alertErrorTitle, 
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
                return null;
            }
        }

        public void SelectWord(string selectedWord)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.SelectWord(_currentUsername, _currentMatchId, selectedWord);
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
            }
        }

        public void SubmitDrawing(byte[] drawingData)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.SubmitDrawing(_currentUsername, _currentMatchId, drawingData);
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
            }
        }

        public void SendInGameMessage(string message)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.SendInGameChatMessage(_currentUsername, _currentMatchId, message);
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
            }
        }

        public void SubmitGuess(string guess, int drawingId)
        {
            if (!IsConnected)
            {
                return;
            }
            try
            {
                _client.SubmitGuess(_currentUsername, _currentMatchId, drawingId, guess);
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
            }
        }

        public void OnRoundStart(int roundNumber, string[] wordOptions)
        {
            if (_isExiting)
            {
                return;
            }

            RoundStart?.Invoke(this, new RoundStartEventArgs { RoundNumber = roundNumber, WordOptions = wordOptions });
        }

        public void OnDrawingPhaseStart(int durationSeconds)
        {
            if (_isExiting)
            {
                return;
            }

            DrawingPhaseStart?.Invoke(this, new DrawingPhaseStartEventArgs { DurationSeconds = durationSeconds });
        }

        public void OnGuessingPhaseStart(DrawingDto drawing)
        {
            if (_isExiting)
            {
                return;
            }

            GuessingPhaseStart?.Invoke(this, new GuessingPhaseStartEventArgs { Drawing = drawing });
        }

        public void OnGameEnd(PlayerScoreDto[] finalScores)
        {
            if (_isExiting)
            {
                return;
            }

            GameEnd?.Invoke(this, new GameEndEventArgs { FinalScores = finalScores });
        }

        public void OnInGameMessageReceived(string sender, string message)
        {
            if (_isExiting)
            {
                return;
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (sender == "SYSTEM")
                {
                    HandleSystemMessage(message);
                }
                else
                {
                    InGameMessageReceived?.Invoke(this, new InGameMessageEventArgs { Sender = sender, Message = message });
                }
            });
        }

        public void OnAnswersPhaseStart(DrawingDto[] allDrawings, GuessDto[] allGuesses, PlayerScoreDto[] currentScores)
        {
            if (_isExiting)
            {
                return;
            }

            AnswersPhaseStart?.Invoke(this, new AnswersPhaseStartEventArgs
            {
                AllDrawings = allDrawings,
                AllGuesses = allGuesses,
                AllScores = currentScores
            });
        }

        public void OnShowNextDrawing(DrawingDto nextDrawing)
        {
            if (_isExiting)
            {
                return;
            }

            ShowNextDrawing?.Invoke(this, new ShowNextDrawingEventArgs { NextDrawing = nextDrawing });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            if (_isExiting)
            {
                return;
            }

            SessionManager.Instance.HandleServerDisconnect();
        }

        private void HandleSystemMessage(string message)
        {
            if (_isExiting)
            {
                return;
            }

            if (message.StartsWith("SYSTEM_LEAVE|"))
            {
                var parts = message.Split('|');
                if (parts.Length > 1)
                {
                    string leaver = parts[1];
                    string format = Lang.gamePlayerLeftMessage;
                    string displayMsg = string.Format(format, leaver);

                    InGameMessageReceived?.Invoke(this, new InGameMessageEventArgs
                    {
                        Sender = Lang.gameSystemTitle,
                        Message = displayMsg
                    });

                    MessageBox.Show(
                        displayMsg,
                        Lang.gameSystemTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else if (message == "SYSTEM_NOT_ENOUGH_PLAYERS")
            {
                MessageBox.Show(
                    Lang.gameEndedNotEnoughPlayers,
                    Lang.gameSystemTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            if (_client != null)
            {
                CleanupConnection();
            }
        }

        public void StartGame(int totalRounds, List<string> players)
        {
            if (!IsConnected) return;
            try
            {
                _client.StartGame(_currentMatchId, totalRounds, players.ToArray());
            }
            catch (Exception)
            {
                SessionManager.Instance.HandleServerDisconnect();
            }
        }
    }

    public class RoundStartEventArgs : EventArgs
    {
        public int RoundNumber { get; set; }
        public string[] WordOptions { get; set; }
    }

    public class DrawingPhaseStartEventArgs : EventArgs
    {
        public int DurationSeconds { get; set; }
    }

    public class InGameMessageEventArgs : EventArgs
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class AnswersPhaseStartEventArgs : EventArgs
    {
        public DrawingDto[] AllDrawings { get; set; }
        public GuessDto[] AllGuesses { get; set; }
        public PlayerScoreDto[] AllScores { get; set; }
    }

    public class ShowNextDrawingEventArgs : EventArgs
    {
        public DrawingDto NextDrawing { get; set; }
    }

    public class GuessingPhaseStartEventArgs : EventArgs
    {
        public DrawingDto Drawing { get; set; }
    }

    public class GameEndEventArgs : EventArgs
    {
        public PlayerScoreDto[] FinalScores { get; set; }
    }
}
