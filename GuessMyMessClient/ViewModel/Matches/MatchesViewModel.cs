using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.WaitingRoom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.LobbyService;
using ServiceMatchFault = GuessMyMessClient.MatchmakingService.ServiceFaultDto;

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
            get
            {
                return _isPublicViewSelected;
            }
            set
            {
                SetProperty(ref _isPublicViewSelected, value);
            }
        }

        public string MatchCode
        {
            get
            {
                return _matchCode;
            }
            set
            {
                SetProperty(ref _matchCode, value);
            }
        }

        private bool _isJoining;
        public bool IsJoining
        {
            get
            {
                return _isJoining;
            }
            set
            {
                if (SetProperty(ref _isJoining, value))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
            }
        }

        public ObservableCollection<MatchInfoModel> PublicMatches
        {
            get
            {
                return _publicMatches;
            }
            set
            {
                SetProperty(ref _publicMatches, value);
            }
        }

        public RelayCommand ShowPublicMatchesCommand { get; private set; }
        public RelayCommand ShowPrivateMatchesCommand { get; private set; }
        public RelayCommand JoinPublicMatchCommand { get; private set; }
        public RelayCommand JoinPrivateMatchCommand { get; private set; }
        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand ReturnCommand { get; private set; }

        public MatchesViewModel()
        {
            PublicMatches = new ObservableCollection<MatchInfoModel>();

            ShowPublicMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = true);
            ShowPrivateMatchesCommand = new RelayCommand((p) => IsPublicViewSelected = false);
            JoinPublicMatchCommand = new RelayCommand(ExecuteJoinPublicMatch, CanExecuteJoin);
            JoinPrivateMatchCommand = new RelayCommand(async (p) => await ExecuteJoinPrivateMatchAsync(p), CanExecuteJoin);
            ReturnCommand = new RelayCommand(ExecuteReturn);

            MatchmakingClientManager.Instance.OnPublicMatchesListUpdated += OnPublicMatchesListUpdated;
            MatchmakingClientManager.Instance.OnMatchJoinedSuccessfully += OnMatchJoined;
            MatchmakingClientManager.Instance.OnMatchmakingFailed += OnMatchmakingFailed;

            Task.Run(() => LoadPublicMatches());
        }

        private bool CanExecuteJoin(object parameter)
        {
            return !IsJoining;
        }

        private async Task LoadPublicMatches()
        {
            try
            {
                var matchesDto = await MatchmakingClientManager.Instance.GetPublicMatchesAsync();
                OnPublicMatchesListUpdated(matchesDto);
            }
            catch (FaultException<ServiceMatchFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading public matches: {ex.Message}");
            }
        }

        private void ExecuteJoinPublicMatch(object parameter)
        {
            if (IsJoining)
            {
                return;
            }
            IsJoining = true;

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
                ShowError(Lang.alertCannotJoinMatch);
                IsJoining = false;
            }
        }

        private async Task ExecuteJoinPrivateMatchAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(MatchCode))
            {
                ShowError(Lang.alertPrivateMatchesErrorNoCode);
                return;
            }

            if (IsJoining)
            {
                return;
            }
            IsJoining = true;

            string codeToJoin = MatchCode.ToUpper();

            try
            {
                var result = await MatchmakingClientManager.Instance.JoinPrivateMatchAsync(codeToJoin);

                if (result.Success && result.Data != null && result.Data.ContainsKey("MatchId"))
                {
                    string matchId = result.Data["MatchId"];
                    JoinLobbyAndNavigate(matchId, true, parameter);
                }
                else
                {
                    ShowError(result.Message ?? Lang.alertJoinMatchError);
                    IsJoining = false;
                }
            }
            catch (FaultException<ServiceMatchFault> fex)
            {
                ShowError(fex.Detail.Message);
                IsJoining = false;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
                IsJoining = false;
            }
            catch (Exception ex)
            {
                ShowError($"{Lang.alertJoinMatchError}: {ex.Message}");
                IsJoining = false;
            }
        }

        private void JoinLobbyAndNavigate(string matchId, bool isPrivate, object parameter)
        {
            var lobbyManager = LobbyClientManager.Instance;
            var sessionManager = SessionManager.Instance;
            string currentUsername = sessionManager.CurrentUsername;

            lobbyManager.Connect(currentUsername, matchId);

            Window currentWindow = FindParentWindow(parameter) ?? _currentWindow;

            if (isPrivate)
            {
                var waitingRoomViewModel = new WaitingRoomPrivateMatchViewModel(lobbyManager, sessionManager);
                var waitingRoomView = new WaitingRoomPrivateMatchView { DataContext = waitingRoomViewModel };
                NavigateToWindow(waitingRoomView, currentWindow);
            }
            else
            {
                var waitingRoomViewModel = new WaitingRoomPublicMatchViewModel(lobbyManager, sessionManager);
                var waitingRoomView = new WaitingRoomPublicMatchView { DataContext = waitingRoomViewModel };
                NavigateToWindow(waitingRoomView, currentWindow);
            }
        }

        private void NavigateToWindow(Window newWindow, Window oldWindow)
        {
            if (oldWindow != null)
            {
                CleanupEvents();
                newWindow.Show();
                oldWindow.Close();
            }
            else
            {
                CleanupEvents();
                newWindow.Show();
                Application.Current.Windows.OfType<View.Matches.MatchesView>().FirstOrDefault()?.Close();
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
            if (matchId != _joiningMatchId || _joiningPrivateMatch)
            {
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (result.Success)
                {
                    JoinLobbyAndNavigate(matchId, false, null);
                }
                else
                {
                    ShowError(result.Message);
                    IsJoining = false;
                }

                _joiningMatchId = null;
                _joiningPrivateMatch = false;
                _currentWindow = null;
            });
        }

        private void OnMatchmakingFailed(string reason)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ShowError($"{Lang.alertMatchmakingError}: {reason}");
                IsJoining = false;
            });
        }

        private void ShowError(string message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show(message, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (commandParameter is Window win)
            {
                return win;
            }
            return Application.Current?.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this || w.IsActive);
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
