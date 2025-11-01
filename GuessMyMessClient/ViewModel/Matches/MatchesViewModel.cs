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
        private bool _isPublicViewSelected = true;
        private string _matchCode;
        private ObservableCollection<MatchInfoModel> _publicMatches;
        private string _joiningMatchId = null;
        private bool _joiningPrivateMatch = false;
        private Window _currentWindow = null;

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

        public RelayCommand ShowPublicMatchesCommand { get; private set; }
        public RelayCommand ShowPrivateMatchesCommand { get; private set; }
        public RelayCommand JoinPublicMatchCommand { get; private set; }
        public RelayCommand JoinPrivateMatchCommand { get; private set; }
        public RelayCommand RefreshCommand { get;  set; }
        public RelayCommand ReturnCommand { get; private set; }

        public MatchesViewModel()
        {
            PublicMatches = new ObservableCollection<MatchInfoModel>();

            ShowPublicMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = true);
            ShowPrivateMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = false);
            JoinPublicMatchCommand = new RelayCommand(ExecuteJoinPublicMatch);
            JoinPrivateMatchCommand = new RelayCommand(async (p) => await ExecuteJoinPrivateMatchAsync(p));
            ReturnCommand = new RelayCommand(ExecuteReturn);

            MatchmakingClientManager.Instance.OnPublicMatchesListUpdated += OnPublicMatchesListUpdated;
            MatchmakingClientManager.Instance.OnMatchJoinedSuccessfully += OnMatchJoined;
            MatchmakingClientManager.Instance.OnMatchmakingFailed += OnMatchmakingFailed;

            LoadPublicMatches();
        }

        private async Task LoadPublicMatches()
        {
            var matchesDto = await MatchmakingClientManager.Instance.GetPublicMatchesAsync();
            OnPublicMatchesListUpdated(matchesDto); 
        }

        private void ExecuteJoinPublicMatch(object parameter) 
        {
            if (parameter is MatchInfoModel matchInfo && matchInfo.CanJoin) 
            {
                _joiningMatchId = matchInfo.MatchId;
                _joiningPrivateMatch = false; 
                _currentWindow = FindParentWindow(); 

                MatchmakingClientManager.Instance.JoinPublicMatch(matchInfo.MatchId);
            }
            else if (parameter is string matchId) 
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

            string codeToJoin = MatchCode.ToUpper();
            var result = await MatchmakingClientManager.Instance.JoinPrivateMatchAsync(codeToJoin);


            if (result.Success && result.Data != null && result.Data.ContainsKey("MatchId"))
            {
                _joiningMatchId = result.Data["MatchId"]; 
                _joiningPrivateMatch = true; 
                _currentWindow = FindParentWindow(parameter); 
                MessageBox.Show($"Solicitud para unirse a partida {codeToJoin} enviada..."); 
            }
            else
            {
                MessageBox.Show(result.Message ?? "Error al intentar unirse a la partida privada.", "Error");
            }
        }

        private void ExecuteReturn(object parameter)
        {
            CleanupEvents(); 
            if (parameter is Window window)
            {
                var lobbyView = new View.Lobby.LobbyView();
                lobbyView.Show();
                window.Close();
            }
        }

        private void OnPublicMatchesListUpdated(List<MatchInfoDto> publicMatches)
        {
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
            if (matchId != _joiningMatchId) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (result.Success)
                {
                    var lobbyManager = LobbyClientManager.Instance; 
                    var sessionManager = SessionManager.Instance; 
                    string currentUsername = sessionManager.CurrentUsername;

                    lobbyManager.Connect(currentUsername, matchId); 

                    Window waitingRoomView = null;
                    ViewModelBase waitingRoomViewModel = null;

                    if (_joiningPrivateMatch) 
                    {
                        waitingRoomViewModel = new WaitingRoomPrivateMatchViewModel(lobbyManager, sessionManager);
                        waitingRoomView = new WaitingRoomPrivateMatchView
                        {
                            DataContext = waitingRoomViewModel
                        };
                    }
                    else 
                    {
                        waitingRoomViewModel = new WaitingRoomPublicMatchViewModel(lobbyManager, sessionManager);
                        waitingRoomView = new WaitingRoomPublicMatchView
                        {
                            DataContext = waitingRoomViewModel
                        };
                    }

                    if (_currentWindow != null && waitingRoomView != null)
                    {
                        CleanupEvents();
                        waitingRoomView.Show();
                        _currentWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show($"¡Unido a la partida {matchId} con éxito! No se pudo navegar.", "Info");
                        lobbyManager.Disconnect(); 
                    }

                    _joiningMatchId = null;
                    _joiningPrivateMatch = false;
                    _currentWindow = null;
                }
                else
                {
                    MessageBox.Show($"Error al unirse a la partida {matchId}: {result.Message}", "Error");
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
            return Application.Current?.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this || w.IsActive);
        }

        private static void OnMatchmakingFailed(string reason)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show($"Error de Matchmaking: {reason}");
            });
        }
    }

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