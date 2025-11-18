using GuessMyMessClient.GameService;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

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
        public event EventHandler<PlayerGuessedEventArgs> PlayerGuessedCorrectly;
        public event EventHandler<TimeUpdateEventArgs> TimeUpdate;
        public event EventHandler<RoundEndEventArgs> RoundEnd;
        public event EventHandler<GameEndEventArgs> GameEnd;
        public event Action ConnectionLost;
        public event EventHandler<InGameMessageEventArgs> InGameMessageReceived;
        public event EventHandler<AnswersPhaseStartEventArgs> AnswersPhaseStart;
        public event EventHandler<ShowNextDrawingEventArgs> ShowNextDrawing;

        public void Connect(string username, string matchId)
        {
            try
            {
                if (IsConnected) Disconnect();

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to GameService: {ex.Message}");
                MessageBox.Show($"Error connecting to the game service: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CleanupConnection();
                ConnectionLost?.Invoke();
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            try
            {
                _client.Disconnect(_currentUsername, _currentMatchId);
                Console.WriteLine($"GameClientManager: Disconnect request sent for {_currentUsername}");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Console.WriteLine($"GameClientManager: Error during disconnect: {ex.Message}. Aborting connection.");
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
                catch (Exception ex)
                {
                    Console.WriteLine($"GameClientManager: Error unsubscribing events: {ex.Message}");
                }

                try
                {
                    if (_client.State != CommunicationState.Faulted) _client.Close();
                    else _client.Abort();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GameClientManager: Error cleaning connection: {ex.Message}");
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                    _currentMatchId = null;
                    _currentUsername = null;
                }
            }
            Console.WriteLine("GameClientManager: Connection cleaned.");
        }

        public async Task<WordDto[]> GetRandomWordsAsync()
        {
            if (!IsConnected) return null;
            try
            {
                return await _client.GetRandomWordsAsync();
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
            if (!IsConnected) return;
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
            if (!IsConnected) return;
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
            if (_client != null && _client.State == CommunicationState.Opened)
            {
                try
                {
                    _client.SendInGameChatMessageAsync(_currentUsername, _currentMatchId, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                    HandleCommunicationError(true);
                }
            }
        }

        public void SubmitGuess(string guess, int drawingId)
        {
            if (_client != null && _client.State == CommunicationState.Opened)
            {
                try
                {
                    _client.SubmitGuessAsync(_currentUsername, _currentMatchId, drawingId, guess);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending guess: {ex.Message}");
                    HandleCommunicationError(true);
                }
            }
        }

        public void OnRoundStart(int roundNumber, string[] wordOptions)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnRoundStart - Round {roundNumber}");
                RoundStart?.Invoke(this, new RoundStartEventArgs { RoundNumber = roundNumber, WordOptions = wordOptions });
            });
        }

        public void OnDrawingPhaseStart(int durationSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnDrawingPhaseStart - {durationSeconds}s");
                DrawingPhaseStart?.Invoke(this, new DrawingPhaseStartEventArgs { DurationSeconds = durationSeconds });
            });
        }

        public void OnGuessingPhaseStart(DrawingDto drawing)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnGuessingPhaseStart - Drawing by {drawing.OwnerUsername}");
                GuessingPhaseStart?.Invoke(this, new GuessingPhaseStartEventArgs { Drawing = drawing });
            });
        }

        public void OnGameEnd(PlayerScoreDto[] finalScores)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Callback: OnGameEnd");
                GameEnd?.Invoke(this, new GameEndEventArgs { FinalScores = finalScores });
            });
        }

        public void OnInGameMessageReceived(string sender, string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                InGameMessageReceived?.Invoke(this, new InGameMessageEventArgs { Sender = sender, Message = message });
            });
        }

        public void OnAnswersPhaseStart(DrawingDto[] allDrawings, GuessDto[] allGuesses, PlayerScoreDto[] currentScores)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Callback: OnAnswersPhaseStart - Showing all answers");
                AnswersPhaseStart?.Invoke(this, new AnswersPhaseStartEventArgs
                {
                    AllDrawings = allDrawings,
                    AllGuesses = allGuesses,
                    AllScores = currentScores
                });
            });
        }

        public void OnShowNextDrawing(DrawingDto nextDrawing)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnShowNextDrawing - Next drawing {nextDrawing.DrawingId}");
                ShowNextDrawing?.Invoke(this, new ShowNextDrawingEventArgs { NextDrawing = nextDrawing });
            });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: WCF channel entered Faulted state.");
            HandleCommunicationError(true);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: WCF channel was closed.");
            if (_client != null)
            {
                HandleCommunicationError(true);
            }
        }

        private void HandleCommunicationError(bool unexpected = false)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("GameClientManager: Handling communication error.");
                CleanupConnection();
                ConnectionLost?.Invoke();
                if (unexpected)
                {
                    MessageBox.Show("The connection to the game service was lost.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

    public class PlayerGuessedEventArgs : EventArgs
    {
        public string Username { get; set; }
    }

    public class TimeUpdateEventArgs : EventArgs
    {
        public int RemainingSeconds { get; set; }
    }

    public class RoundEndEventArgs : EventArgs
    {
        public PlayerScoreDto[] RoundScores { get; set; }
        public string CorrectWord { get; set; }
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
