using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.WaitingRoom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Matches
{
    public class MatchesViewModel : ViewModelBase
    {
        // --- Campos ---
        private bool _isPublicViewSelected = true;
        private string _matchCode;
        private ObservableCollection<MatchInfoModel> _publicMatches;
        private string _joiningMatchId = null;
        private bool _joiningPrivateMatch = false;
        private Window _currentWindow = null;

        // --- Propiedades de Binding ---
        public bool IsPublicViewSelected
        {
            get => _isPublicViewSelected;
            set => SetProperty(ref _isPublicViewSelected, value);
        }

        public string MatchCode
        {
            get => _matchCode;
            set => SetProperty(ref _matchCode, value);
        }

        public ObservableCollection<MatchInfoModel> PublicMatches
        {
            get => _publicMatches;
            set => SetProperty(ref _publicMatches, value);
        }

        // --- Comandos ---
        public RelayCommand ShowPublicMatchesCommand { get; private set; }
        public RelayCommand ShowPrivateMatchesCommand { get; private set; }
        public RelayCommand JoinPublicMatchCommand { get; private set; }
        public RelayCommand JoinPrivateMatchCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand ReturnCommand { get; private set; }
        // ... (comandos de ventana) ...

        public MatchesViewModel()
        {
            PublicMatches = new ObservableCollection<MatchInfoModel>();

            ShowPublicMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = true);
            ShowPrivateMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = false);
            JoinPublicMatchCommand = new RelayCommand(ExecuteJoinPublicMatch);
            JoinPrivateMatchCommand = new RelayCommand(async (p) => await ExecuteJoinPrivateMatchAsync(p));
            ReturnCommand = new RelayCommand(ExecuteReturn);
            // ... (inicializar comandos de ventana) ...

            // Suscribirse a los eventos del Manager
            // CORREGIDO: Se agregó "On" al nombre del evento
            MatchmakingClientManager.Instance.OnPublicMatchesListUpdated += OnPublicMatchesListUpdated;
            // CORREGIDO: Se agregó "On" al nombre del evento
            MatchmakingClientManager.Instance.OnMatchJoinedSuccessfully += OnMatchJoined;
            // CORREGIDO: Se agregó "On" al nombre del evento
            MatchmakingClientManager.Instance.OnMatchmakingFailed += OnMatchmakingFailed;

            // Cargar la lista inicial
            LoadPublicMatches();
        }

        private async void LoadPublicMatches()
        {
            var matchesDto = await MatchmakingClientManager.Instance.GetPublicMatchesAsync();
            OnPublicMatchesListUpdated(matchesDto); // Reutiliza el handler del evento
        }

        private void ExecuteJoinPublicMatch(object parameter) // Cambiado para recibir el objeto completo o su VM
        {
            if (parameter is MatchInfoModel matchInfo && matchInfo.CanJoin) // Asume que pasas el MatchInfoModel
            {
                // Guarda la información antes de llamar al servicio
                _joiningMatchId = matchInfo.MatchId;
                _joiningPrivateMatch = false; // Sabemos que es pública
                _currentWindow = FindParentWindow(); // Intenta obtener la ventana actual

                MatchmakingClientManager.Instance.JoinPublicMatch(matchInfo.MatchId);
                // La respuesta y navegación se manejan en OnMatchJoined
            }
            else if (parameter is string matchId) // Fallback si solo pasas el ID
            {
                _joiningMatchId = matchId;
                _joiningPrivateMatch = false;
                _currentWindow = FindParentWindow();
                MatchmakingClientManager.Instance.JoinPublicMatch(matchId);
            }
            else
            {
                MessageBox.Show("No se puede unir a la partida seleccionada.", "Error");
            }
        }

        private async Task ExecuteJoinPrivateMatchAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(MatchCode))
            {
                MessageBox.Show(Properties.Langs.Lang.alertPrivateMatchesErrorNoCode);
                return;
            }

            string codeToJoin = MatchCode.ToUpper(); // Guarda el código antes de la llamada async

            // Muestra indicador de carga si es necesario
            // IsBusy = true;

            var result = await MatchmakingClientManager.Instance.JoinPrivateMatchAsync(codeToJoin);

            // IsBusy = false;

            if (result.Success && result.Data != null && result.Data.ContainsKey("MatchId"))
            {
                // Guarda la información ANTES de que el callback OnMatchJoined se dispare
                _joiningMatchId = result.Data["MatchId"]; // El ID real de la partida
                _joiningPrivateMatch = true; // Sabemos que es privada
                _currentWindow = FindParentWindow(parameter); // Intenta obtener la ventana actual
                // NO navegues aquí, espera al callback OnMatchJoined que confirma la unión en el servidor
                MessageBox.Show($"Solicitud para unirse a partida {codeToJoin} enviada..."); // Feedback temporal
            }
            else
            {
                MessageBox.Show(result.Message ?? "Error al intentar unirse a la partida privada.", "Error");
            }
        }

        private void ExecuteReturn(object parameter)
        {
            CleanupEvents(); // Llama a la limpieza de eventos
            if (parameter is Window window)
            {
                var lobbyView = new View.Lobby.LobbyView();
                lobbyView.Show();
                window.Close();
            }
        }

        // --- Handlers de Eventos del ClientManager ---

        private void OnPublicMatchesListUpdated(List<MatchInfoDto> publicMatches)
        {
            // Asegurarse de que esto se ejecute en el hilo de la UI
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                PublicMatches.Clear();
                if (publicMatches != null)
                {
                    foreach (var matchDto in publicMatches)
                    {
                        PublicMatches.Add(new MatchInfoModel(matchDto));
                    }
                }
            });
        }

        private void OnMatchJoined(string matchId, OperationResultDto result)
        {
            // Solo proceder si este callback corresponde a la partida que intentamos unir
            if (matchId != _joiningMatchId) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (result.Success)
                {
                    // --- NAVEGACIÓN Y CONEXIÓN ---
                    // 1. Obtener instancias
                    var lobbyManager = LobbyClientManager.Instance; // O por DI
                    var sessionManager = SessionManager.Instance; // O por DI
                    string currentUsername = sessionManager.CurrentUsername;

                    // 2. Conectar al servicio de Lobby
                    lobbyManager.Connect(currentUsername, matchId); // Usa el ID único siempre

                    // 3. Crear ViewModel y Vista correspondientes (JUGADOR)
                    Window waitingRoomView = null;
                    ViewModelBase waitingRoomViewModel = null;

                    if (_joiningPrivateMatch) // Usa el flag guardado
                    {
                        waitingRoomViewModel = new WaitingRoomPrivateMatchViewModel(lobbyManager, sessionManager);
                        waitingRoomView = new WaitingRoomPrivateMatchView
                        {
                            DataContext = waitingRoomViewModel
                        };
                    }
                    else // Es pública
                    {
                        waitingRoomViewModel = new WaitingRoomPublicMatchViewModel(lobbyManager, sessionManager);
                        waitingRoomView = new WaitingRoomPublicMatchView
                        {
                            DataContext = waitingRoomViewModel
                        };
                    }

                    // 4. Mostrar nueva ventana y cerrar la actual
                    if (_currentWindow != null && waitingRoomView != null)
                    {
                        // Desuscribirse ANTES de cerrar la ventana
                        CleanupEvents();
                        waitingRoomView.Show();
                        _currentWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show($"¡Unido a la partida {matchId} con éxito! No se pudo navegar.", "Info");
                        lobbyManager.Disconnect(); // Desconectar si no navegamos
                    }

                    // Limpiar flags
                    _joiningMatchId = null;
                    _joiningPrivateMatch = false;
                    _currentWindow = null;
                    // --- FIN NAVEGACIÓN Y CONEXIÓN ---
                }
                else
                {
                    MessageBox.Show($"Error al unirse a la partida {matchId}: {result.Message}", "Error");
                    // Limpiar flags si falla
                    _joiningMatchId = null;
                    _joiningPrivateMatch = false;
                    _currentWindow = null;
                }
            });
        }

        private void CleanupEvents()
        {
            MatchmakingClientManager.Instance.OnPublicMatchesListUpdated -= OnPublicMatchesListUpdated;
            MatchmakingClientManager.Instance.OnMatchJoinedSuccessfully -= OnMatchJoined;
            MatchmakingClientManager.Instance.OnMatchmakingFailed -= OnMatchmakingFailed;
        }

        private Window FindParentWindow(object commandParameter = null)
        {
            if (commandParameter is Window win) return win;

            // Intenta encontrar la ventana activa asociada a este ViewModel
            return Application.Current?.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this || w.IsActive);
        }

        private void OnMatchmakingFailed(string reason)
        {
            // Asegurarse de que esto se ejecute en el hilo de la UI
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show($"Error de Matchmaking: {reason}");
            });
        }
    }

    /// <summary>
    /// Modelo de cliente para mostrar en la lista de partidas públicas.
    /// </summary>
    public class MatchInfoModel : ViewModelBase
    {
        public string MatchId { get; set; }
        public string MatchName { get; set; }
        public string HostUsername { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string DifficultyName { get; set; }
        public bool CanJoin => CurrentPlayers < MaxPlayers;

        public MatchInfoModel(MatchInfoDto dto)
        {
            MatchId = dto.MatchId;
            MatchName = dto.MatchName;
            HostUsername = dto.HostUsername;
            CurrentPlayers = dto.CurrentPlayers;
            MaxPlayers = dto.MaxPlayers;
            DifficultyName = dto.DifficultyName;
        }
    }
}