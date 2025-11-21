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

        public void Connect(string username, string matchId)
        {
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
                Console.WriteLine($"GameClientManager: Connected as {username} to match {matchId}");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to GameService: {ex.Message}");
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
                    Console.WriteLine($"GameClientManager: Disconnect request sent for {_currentUsername}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameClientManager: Error during disconnect: {ex.Message}");
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
                    Console.WriteLine($"GameClientManager: Error closing client: {ex.Message}");
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                }
            }
            Console.WriteLine("GameClientManager: Connection cleaned.");
        }

        public async Task<WordDto[]> GetRandomWordsAsync()
        {
            if (!IsConnected)
            {
                return null;
            }

            try
            {
                return await _client.GetRandomWordsAsync();
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                MessageBox.Show(fex.Detail.Message, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRandomWordsAsync: {ex.Message}");
                HandleCommunicationError(true);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SelectWord: {ex.Message}");
                HandleCommunicationError(true);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitDrawing: {ex.Message}");
                HandleCommunicationError(true);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                HandleCommunicationError(true);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending guess: {ex.Message}");
                HandleCommunicationError(true);
            }
        }

        public void OnRoundStart(int roundNumber, string[] wordOptions)
        {
            Console.WriteLine($"Callback: OnRoundStart - Round {roundNumber}");
            RoundStart?.Invoke(this, new RoundStartEventArgs { RoundNumber = roundNumber, WordOptions = wordOptions });
        }

        public void OnDrawingPhaseStart(int durationSeconds)
        {
            Console.WriteLine($"Callback: OnDrawingPhaseStart - {durationSeconds}s");
            DrawingPhaseStart?.Invoke(this, new DrawingPhaseStartEventArgs { DurationSeconds = durationSeconds });
        }

        public void OnGuessingPhaseStart(DrawingDto drawing)
        {
            Console.WriteLine($"Callback: OnGuessingPhaseStart - Drawing by {drawing.OwnerUsername}");
            GuessingPhaseStart?.Invoke(this, new GuessingPhaseStartEventArgs { Drawing = drawing });
        }

        public void OnGameEnd(PlayerScoreDto[] finalScores)
        {
            Console.WriteLine("Callback: OnGameEnd");
            GameEnd?.Invoke(this, new GameEndEventArgs { FinalScores = finalScores });
        }

        public void OnInGameMessageReceived(string sender, string message)
        {
            InGameMessageReceived?.Invoke(this, new InGameMessageEventArgs { Sender = sender, Message = message });
        }

        public void OnAnswersPhaseStart(DrawingDto[] allDrawings, GuessDto[] allGuesses, PlayerScoreDto[] currentScores)
        {
            Console.WriteLine("Callback: OnAnswersPhaseStart");
            AnswersPhaseStart?.Invoke(this, new AnswersPhaseStartEventArgs
            {
                AllDrawings = allDrawings,
                AllGuesses = allGuesses,
                AllScores = currentScores
            });
        }

        public void OnShowNextDrawing(DrawingDto nextDrawing)
        {
            Console.WriteLine($"Callback: OnShowNextDrawing - Next {nextDrawing.DrawingId}");
            ShowNextDrawing?.Invoke(this, new ShowNextDrawingEventArgs { NextDrawing = nextDrawing });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: WCF channel faulted.");
            HandleCommunicationError(true);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: WCF channel closed.");
            if (_client != null)
            {
                HandleCommunicationError(false);
            }
        }

        private void HandleCommunicationError(bool showMessage)
        {
            if (_client == null)
            {
                return;
            }

            CleanupConnection();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (showMessage)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertConnectionErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                ConnectionLost?.Invoke();
            });
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
