using GuessMyMessClient.GameService; // Namespace de tu referencia de servicio
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Session
{
    // 1. Implementamos la interfaz de Callback
    public class GameClientManager : IGameServiceCallback
    {
        // 2. Patrón Singleton (como en tus ejemplos)
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

        // 4. Eventos para los Callbacks (lo que los ViewModels escucharán)
        public event Action<int, string[]> RoundStart;
        public event Action<int> DrawingPhaseStart;
        public event Action<byte[], string> GuessingPhaseStart;
        public event Action<string> PlayerGuessedCorrectly;
        public event Action<int> TimeUpdate;
        public event Action<PlayerScoreDto[], string> RoundEnd;
        public event Action<PlayerScoreDto[]> GameEnd;
        public event Action ConnectionLost;


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

                // Llamamos al "Connect" del servidor
                _client.Connect(_currentUsername);
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
                // Llamamos al "Disconnect" del servidor
                _client.Disconnect(_currentUsername);
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
                // ¡Este es el método que necesitas!
                return await _client.GetRandomWordsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetRandomWordsAsync: {ex.Message}");
                HandleCommunicationError();
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
                HandleCommunicationError();
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
                HandleCommunicationError();
            }
        }

        public void SubmitGuess(string guess)
        {
            if (!IsConnected) return;
            try
            {
                _client.SubmitGuess(_currentUsername, _currentMatchId, guess);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en SubmitGuess: {ex.Message}");
                HandleCommunicationError();
            }
        }

        public void SendInGameChatMessage(string message)
        {
            if (!IsConnected) return;
            try
            {
                _client.SendInGameChatMessage(_currentUsername, _currentMatchId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en SendInGameChatMessage: {ex.Message}");
                HandleCommunicationError();
            }
        }


        // 7. Implementación de Callbacks (invocan los eventos)

        public void OnRoundStart(int roundNumber, string[] wordOptions)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnRoundStart - Ronda {roundNumber}");
                RoundStart?.Invoke(roundNumber, wordOptions);
            });
        }

        public void OnDrawingPhaseStart(int durationSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnDrawingPhaseStart - {durationSeconds}s");
                DrawingPhaseStart?.Invoke(durationSeconds);
            });
        }

        public void OnGuessingPhaseStart(byte[] drawingData, string artistUsername)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnGuessingPhaseStart - Artista: {artistUsername}");
                GuessingPhaseStart?.Invoke(drawingData, artistUsername);
            });
        }

        public void OnPlayerGuessedCorrectly(string username)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnPlayerGuessedCorrectly - {username}");
                PlayerGuessedCorrectly?.Invoke(username);
            });
        }

        public void OnTimeUpdate(int remainingSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Evitar spam en consola, comentar si es necesario
                // Console.WriteLine($"Callback: OnTimeUpdate - {remainingSeconds}s");
                TimeUpdate?.Invoke(remainingSeconds);
            });
        }

        public void OnRoundEnd(PlayerScoreDto[] roundScores, string correctWord)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Callback: OnRoundEnd - Palabra: {correctWord}");
                RoundEnd?.Invoke(roundScores, correctWord);
            });
        }

        public void OnGameEnd(PlayerScoreDto[] finalScores)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Callback: OnGameEnd");
                GameEnd?.Invoke(finalScores);
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
            // Solo manejamos como error si no estábamos ya desconectándonos
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
}