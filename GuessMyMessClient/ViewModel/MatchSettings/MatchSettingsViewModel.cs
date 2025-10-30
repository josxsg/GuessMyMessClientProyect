using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.ViewModel.Session;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.WaitingRoom;

namespace GuessMyMessClient.ViewModel.MatchSettings
{
    public class MatchSettingsViewModel : ViewModelBase
    {
        private string _matchName = "Partida de " + SessionManager.Instance.CurrentUsername;
        private bool _isPrivateMatch = false;
        private int _maxPlayers;
        private int _rounds;
        private DifficultyModel _selectedDifficulty;

        public List<int> AvailablePlayerCounts { get; set; }
        public List<int> AvailableRoundCounts { get; set; }
        public List<DifficultyModel> AvailableDifficulties { get; set; }

        public string MatchName
        {
            get => _matchName;
            set => SetProperty(ref _matchName, value);
        }

        public bool IsPublicMatch
        {
            get => !_isPrivateMatch;
            set
            {
                if (SetProperty(ref _isPrivateMatch, !value)) 
                {
                    OnPropertyChanged(nameof(IsPublicMatch)); 
                }
            }
        }

        public int MaxPlayers
        {
            get => _maxPlayers;
            set => SetProperty(ref _maxPlayers, value);
        }

        public int Rounds
        {
            get => _rounds;
            set => SetProperty(ref _rounds, value);
        }

        public DifficultyModel SelectedDifficulty
        {
            get => _selectedDifficulty;
            set => SetProperty(ref _selectedDifficulty, value);
        }

        public RelayCommand SetMatchTypeCommand { get; private set; }
        public RelayCommand CreateMatchCommand { get; private set; }
        public RelayCommand ReturnCommand { get; private set; }

        public MatchSettingsViewModel()
        {
            AvailablePlayerCounts = Enumerable.Range(2, 7).ToList();
            AvailableRoundCounts = Enumerable.Range(3, 4).ToList();
            AvailableDifficulties = new List<DifficultyModel>
            {
                new DifficultyModel { Id = 1, Name = Properties.Langs.Lang.createGameCbEasy },
                new DifficultyModel { Id = 2, Name = Properties.Langs.Lang.createGameCbIntermediate },
                new DifficultyModel { Id = 3, Name = Properties.Langs.Lang.createGameCbHard }
            };

            MaxPlayers = AvailablePlayerCounts.First();
            Rounds = AvailableRoundCounts.First();
            SelectedDifficulty = AvailableDifficulties.First();

            SetMatchTypeCommand = new RelayCommand(ExecuteSetMatchType);
            CreateMatchCommand = new RelayCommand(async (param) => await ExecuteCreateMatchAsync(param));
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }

        private void ExecuteSetMatchType(object parameter)
        {
            IsPublicMatch = (parameter.ToString() == "Public");
        }

        private async Task ExecuteCreateMatchAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(MatchName))
            {
                MessageBox.Show(Properties.Langs.Lang.alertCreateGameErrorName);
                return;
            }

            var settings = new LobbySettingsDto
            {
                MatchName = this.MatchName,
                IsPrivate = this._isPrivateMatch,
                MaxPlayers = this.MaxPlayers,
                TotalRounds = this.Rounds, // Asegúrate que el servidor maneje esto si es necesario
                DifficultyId = this.SelectedDifficulty.Id
            };

            // Muestra un indicador de carga si es necesario
            // IsBusy = true;

            var result = await MatchmakingClientManager.Instance.CreateMatchAsync(settings);

            // IsBusy = false;

            if (result.Success && result.Data != null && result.Data.ContainsKey("MatchId"))
            {
                string matchId = result.Data["MatchId"]; // El ID único de la partida
                                                         // string matchCode = result.Data.ContainsKey("MatchCode") ? result.Data["MatchCode"] : null; // Código si es privada

                // --- NAVEGACIÓN Y CONEXIÓN ---
                // 1. Obtener instancias necesarias
                var lobbyManager = LobbyClientManager.Instance; // O obténlo por DI
                var sessionManager = SessionManager.Instance; // O obténlo por DI
                string currentUsername = sessionManager.CurrentUsername;

                // 2. Conectar al servicio de Lobby ANTES de mostrar la ventana
                lobbyManager.Connect(currentUsername, matchId); // Usa el ID único siempre para conectar

                // 3. Crear ViewModel y Vista correspondientes
                Window waitingRoomView = null;
                ViewModelBase waitingRoomViewModel = null;

                if (_isPrivateMatch)
                {
                    waitingRoomViewModel = new WaitingRoomPrivateMatchHostViewModel(lobbyManager, sessionManager);
                    waitingRoomView = new WaitingRoomPrivateMatchHostView
                    {
                        DataContext = waitingRoomViewModel
                    };
                }
                else // Es pública
                {
                    waitingRoomViewModel = new WaitingRoomPublicMatchHostViewModel(lobbyManager, sessionManager);
                    waitingRoomView = new WaitingRoomPublicMatchHostView
                    {
                        DataContext = waitingRoomViewModel
                    };
                }

                // 4. Mostrar nueva ventana y cerrar la actual
                if (parameter is Window currentWindow && waitingRoomView != null)
                {
                    waitingRoomView.Show();
                    currentWindow.Close();
                }
                else
                {
                    // Fallback si no se pasó la ventana actual o algo falló
                    MessageBox.Show($"Partida creada (ID: {matchId}). No se pudo navegar automáticamente.", "Info");
                    lobbyManager.Disconnect(); // Desconectar si no pudimos navegar
                }
                // --- FIN NAVEGACIÓN Y CONEXIÓN ---
            }
            else
            {
                MessageBox.Show(result.Message ?? "Error desconocido al crear la partida.", "Error");
            }
        }

        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window window)
            {
                var lobbyView = new View.Lobby.LobbyView();
                lobbyView.Show();
                window.Close();
            }
        }
    }

    public class DifficultyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
