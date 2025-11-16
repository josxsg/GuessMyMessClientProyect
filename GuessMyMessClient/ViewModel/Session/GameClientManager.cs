using GuessMyMessClient.GameService; // Namespace de tu referencia de servicio
using System;
using System.Collections.Generic; // Sigue siendo necesario para los EventArgs
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Session
{
    // 1. Implementamos la interfaz de Callback
    public class GameClientManager : IGameServiceCallback
    {
        // 2. Patrón Singleton
        private static readonly Lazy<GameClientManager> _lazyInstance =
            new Lazy<GameClientManager>(() => new GameClientManager());

        public static GameClientManager Instance => _lazyInstance.Value;

        private GameClientManager() { }

        // 3. Cliente WCF y estado
        private GameServiceClient _client;
        private string _currentUsername;
        private string _currentMatchId;
        private const string ENDPOINT_NAME = "NetTcpBinding_IGameService"; // De tu App.config

        public bool IsConnected => _client != null && _client.State == CommunicationState.Opened;

        // 4. Eventos para los Callbacks (CORREGIDOS)
        // Eliminadas las definiciones antiguas y duplicadas.
        public event EventHandler<RoundStartEventArgs> RoundStart;
        public event EventHandler<DrawingPhaseStartEventArgs> DrawingPhaseStart;
        public event EventHandler<GuessingPhaseStartEventArgs> GuessingPhaseStart; // Corregido
        public event EventHandler<PlayerGuessedEventArgs> PlayerGuessedCorrectly;
        public event EventHandler<TimeUpdateEventArgs> TimeUpdate;
        public event EventHandler<RoundEndEventArgs> RoundEnd;
        public event EventHandler<GameEndEventArgs> GameEnd; // Corregido (solo una definición)
        public event Action ConnectionLost;
        public event EventHandler<InGameMessageEventArgs> InGameMessageReceived;
        public event EventHandler<ShowAnswersEventArgs> ShowAnswersPhase;
        public event EventHandler<ShowNextDrawingEventArgs> ShowNextDrawing;


        // 5. Métodos de Ciclo de Vida (Conexión / Desconexión)
        public void Connect(string username, string matchId)
        {
            try
            {
                if (IsConnected) Disconnect();

                _currentUsername = username;
                _currentMatchId = matchId;

                var instanceContext = new InstanceContext(this);
                _client = new GameServiceClient(instanceContext, ENDPOINT_NAME);
                _client.Open();

                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;

                _client.Connect(_currentUsername, _currentMatchId);
                Console.WriteLine($"GameClientManager: Conectado como {username} a la partida {matchId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error conectando a GameService: {ex.Message}");
                MessageBox.Show($"Error al conectar con el servicio de juego: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Console.WriteLine($"GameClientManager: Enviada solicitud de desconexión para {_currentUsername}");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Console.WriteLine($"GameClientManager: Error al desconectar limpiamente: {ex.Message}. Abortando.");
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
                    Console.WriteLine($"GameClientManager: Error al desuscribir eventos: {ex.Message}");
                }

                try
                {
                    if (_client.State != CommunicationState.Faulted) _client.Close();
                    else _client.Abort();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GameClientManager: Excepción al limpiar conexión: {ex.Message}");
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                    _currentMatchId = null;
                    _currentUsername = null;
                }
            }
            Console.WriteLine("GameClientManager: Conexión limpiada.");
        }


        // 6. Métodos públicos (Wrappers para llamar al servidor)

        public async Task<WordDto[]> GetRandomWordsAsync()
        {
            if (!IsConnected) return null;
            try
            {
                return await _client.GetRandomWordsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetRandomWordsAsync: {ex.Message}");
                HandleCommunicationError(true); // Manejo de error unificado
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
                Console.WriteLine($"Error en SelectWord: {ex.Message}");
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
                Console.WriteLine($"Error en SubmitDrawing: {ex.Message}");
                HandleCommunicationError(true);
            }
        }

        public void SendInGameMessage(string message)
        {
            if (_client != null && _client.State == CommunicationState.Opened)
            {
                try
                {
                    // CORREGIDO: Usando _currentUsername y _currentMatchId
                    // CORREGIDO: Llamada Async (asumiendo que actualizaste tu referencia de servicio)
                    // Si no es Async, quita el "Async" al final.
                    _client.SendInGameChatMessageAsync(_currentUsername, _currentMatchId, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar mensaje: {ex.Message}");
                    // CORREGIDO: Usando HandleCommunicationError
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
                    // CORREGIDO: Usando _currentUsername y _currentMatchId
                    // CORREGIDO: Llamada Async
                    _client.SubmitGuessAsync(_currentUsername, _currentMatchId, drawingId, guess);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar guess: {ex.Message}");
                    // CORREGIDO: Usando HandleCommunicationError
                    HandleCommunicationError(true);
                }
            }
        }


        // 7. Implementación de Callbacks (TODOS LOS MÉTODOS DE LA INTERFAZ)

        public void OnRoundStart(int roundNumber, string[] wordOptions)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnRoundStart - Ronda {roundNumber}");
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
                Console.WriteLine($"Callback: OnGuessingPhaseStart - Dibujo de {drawing.OwnerUsername}");
                // CORREGIDO: Esto ahora coincide con el evento y el EventArgs
                GuessingPhaseStart?.Invoke(this, new GuessingPhaseStartEventArgs { Drawing = drawing });
            });
        }

        // --- AÑADIDOS LOS MÉTODOS FALTANTES ---

        public void OnPlayerGuessedCorrectly(string username)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnPlayerGuessedCorrectly - {username}");
                PlayerGuessedCorrectly?.Invoke(this, new PlayerGuessedEventArgs { Username = username });
            });
        }

        public void OnTimeUpdate(int remainingSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TimeUpdate?.Invoke(this, new TimeUpdateEventArgs { RemainingSeconds = remainingSeconds });
            });
        }

        public void OnRoundEnd(PlayerScoreDto[] roundScores, string correctWord)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnRoundEnd - La palabra era {correctWord}");
                RoundEnd?.Invoke(this, new RoundEndEventArgs { RoundScores = roundScores, CorrectWord = correctWord });
            });
        }

        public void OnGameEnd(PlayerScoreDto[] finalScores)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Callback: OnGameEnd");
                // CORREGIDO: WCF usa arrays (T[]) no Listas (List<T>)
                GameEnd?.Invoke(this, new GameEndEventArgs { FinalScores = finalScores });
            });
        }

        // --- MÉTODOS DE LOS NUEVOS CALLBACKS ---

        public void OnInGameMessageReceived(string sender, string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                InGameMessageReceived?.Invoke(this, new InGameMessageEventArgs { Sender = sender, Message = message });
            });
        }

        public void OnShowAnswers(DrawingDto drawing, GuessDto[] guesses, PlayerScoreDto[] scores)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnShowAnswers - Mostrando respuestas para {drawing.OwnerUsername}");
                // CORREGIDO: WCF usa arrays (T[]) no Listas (List<T>)
                ShowAnswersPhase?.Invoke(this, new ShowAnswersEventArgs { Drawing = drawing, Guesses = guesses, Scores = scores });
            });
        }

        public void OnShowNextDrawing(DrawingDto nextDrawing)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnShowNextDrawing - Siguiente dibujo {nextDrawing.DrawingId}");
                ShowNextDrawing?.Invoke(this, new ShowNextDrawingEventArgs { NextDrawing = nextDrawing });
            });
        }


        // 8. Manejadores de Errores de Canal
        private void Channel_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: Canal WCF ha entrado en estado Faulted.");
            HandleCommunicationError(true);
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("GameClientManager: Canal WCF fue cerrado.");
            if (_client != null)
            {
                HandleCommunicationError(true);
            }
        }

        private void HandleCommunicationError(bool unexpected = false)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("GameClientManager: Manejando error de comunicación.");
                CleanupConnection();
                ConnectionLost?.Invoke();
                if (unexpected)
                {
                    MessageBox.Show("Se perdió la conexión con el servicio de juego.", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }
    }

    // --- DEFINICIÓN DE TODOS LOS EVENT ARGS ---

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

    public class ShowAnswersEventArgs : EventArgs
    {
        public DrawingDto Drawing { get; set; }
        // CORREGIDO: Debe ser array para coincidir con el callback
        public GuessDto[] Guesses { get; set; }
        public PlayerScoreDto[] Scores { get; set; }
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
        // CORREGIDO: Debe ser array para coincidir con el callback
        public PlayerScoreDto[] FinalScores { get; set; }
    }
}