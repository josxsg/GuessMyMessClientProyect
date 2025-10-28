using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.ViewModel.Session;
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

        private void ExecuteJoinPublicMatch(object matchId)
        {
            if (matchId is string id)
            {
                MatchmakingClientManager.Instance.JoinPublicMatch(id);
                // La respuesta se maneja en el evento OnMatchJoined
            }
        }

        private async Task ExecuteJoinPrivateMatchAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(MatchCode))
            {
                MessageBox.Show(Properties.Langs.Lang.alertPrivateMatchesErrorNoCode); // Necesitarás este Lang
                return;
            }

            var result = await MatchmakingClientManager.Instance.JoinPrivateMatchAsync(MatchCode.ToUpper());

            if (result.Success)
            {
                // El evento OnMatchJoined se disparará por el callback
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window window)
            {
                // Desuscribirse de eventos
                // CORREGIDO: Se agregó "On" al nombre del evento
                MatchmakingClientManager.Instance.OnPublicMatchesListUpdated -= OnPublicMatchesListUpdated;
                // CORREGIDO: Se agregó "On" al nombre del evento
                MatchmakingClientManager.Instance.OnMatchJoinedSuccessfully -= OnMatchJoined;
                // CORREGIDO: Se agregó "On" al nombre del evento
                MatchmakingClientManager.Instance.OnMatchmakingFailed -= OnMatchmakingFailed;

                // Volver al Lobby (menú principal)
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
            // Asegurarse de que esto se ejecute en el hilo de la UI
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (result.Success)
                {
                    MessageBox.Show($"¡Unido a la partida {matchId} con éxito!");
                    // TODO: Navegar a la vista de WaitingRoom (no-host)
                    // var waitingRoom = new WaitingRoomView(matchId);
                    // ... (cerrar ventana actual y mostrar la nueva) ...
                }
                else
                {
                    MessageBox.Show($"Error al unirse: {result.Message}");
                }
            });
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