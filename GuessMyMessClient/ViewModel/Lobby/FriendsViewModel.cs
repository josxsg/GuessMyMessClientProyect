using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.Properties.Langs;
using ServiceSocialFault = GuessMyMessClient.SocialService.ServiceFaultDto;
using GuessMyMessClient.View.Lobby;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class FriendsViewModel : ViewModelBase, IDisposable
    {
        private static SocialServiceClient Client => SocialClientManager.Instance.Client;

        public ObservableCollection<FriendViewModel> Friends { get; }
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }

        private ObservableCollection<UserProfileDto> _searchResults;
        public ObservableCollection<UserProfileDto> SearchResults
        {
            get
            {
                return _searchResults;
            }
            set
            {
                if (_searchResults != value)
                {
                    _searchResults = value; OnPropertyChanged();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value; OnPropertyChanged();
                }
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand SendFriendRequestCommand { get; }
        public ICommand RemoveFriendCommand { get; }
        public ICommand ViewFriendProfileCommand { get; }

        public FriendsViewModel()
        {
            Friends = new ObservableCollection<FriendViewModel>();
            FriendRequests = new ObservableCollection<FriendRequestViewModel>();
            SearchResults = new ObservableCollection<UserProfileDto>();

            SearchCommand = new RelayCommand(async parameter => await SearchUsersAsync(), parameter => CanExecuteNetworkActions());
            SendFriendRequestCommand = new RelayCommand(SendFriendRequest, parameter => CanExecuteNetworkActions());
            RemoveFriendCommand = new RelayCommand(async (p) => await RemoveFriendAsync(p), (p) => CanExecuteNetworkActions());
            ViewFriendProfileCommand = new RelayCommand(ViewFriendProfile);

            SubscribeToEvents();

            if (CanExecuteNetworkActions())
            {
                Task.Run(() => LoadFriendsAndRequestsAsync());
            }
        }

        private static bool CanExecuteNetworkActions()
        {
            return Client != null && Client.State == CommunicationState.Opened;
        }

        private async Task LoadFriendsAndRequestsAsync()
        {
            string username = SessionManager.Instance.CurrentUsername;

            if (string.IsNullOrEmpty(username) || !CanExecuteNetworkActions())
            {
                return;
            }

            try
            {
                var friends = await Client.GetFriendsListAsync(username);
                var requests = await Client.GetFriendRequestsAsync(username);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateFriendsList(friends);
                    UpdateFriendRequestsList(requests);
                });
            }
            catch (Exception ex)
            {
                HandleSocialException(ex);
            }
        }

        private void UpdateFriendsList(FriendDto[] friends)
        {
            Friends.Clear();
            if (friends == null) return;

            foreach (var f in friends)
            {
                Friends.Add(new FriendViewModel
                {
                    Username = f.Username,
                    IsOnline = f.IsOnline
                });
            }
        }

        private void UpdateFriendRequestsList(FriendRequestInfoDto[] requests)
        {
            FriendRequests.Clear();
            if (requests == null) return;

            foreach (var r in requests)
            {
                FriendRequests.Add(new FriendRequestViewModel(this)
                {
                    RequesterUsername = r.RequesterUsername
                });
            }
        }

        private void HandleSocialException(Exception ex)
        {
            if (ex is FaultException<ServiceSocialFault> fex)
            {
                ShowServiceError((AuthService.ServiceErrorType)fex.Detail.ErrorType);
            }
            else
            {
                HandleException(ex);
            }
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await Application.Current.Dispatcher.InvokeAsync(() => SearchResults.Clear());
                return;
            }

            if (!CanExecuteNetworkActions())
            {
                return;
            }

            try
            {
                var users = await Client.SearchUsersAsync(SearchText, SessionManager.Instance.CurrentUsername);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SearchResults.Clear();
                    if (users != null)
                    {
                        foreach (var u in users)
                        {
                            SearchResults.Add(u);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                HandleSocialException(ex);
            }
        }

        private void SendFriendRequest(object parameter)
        {
            if (!(parameter is UserProfileDto userProfile) || !CanExecuteNetworkActions())
            {
                return;
            }

            try
            {
                Client.SendFriendRequest(SessionManager.Instance.CurrentUsername, userProfile.Username);

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SearchResults.Remove(userProfile);
                    ShowAlert(string.Format(Lang.alertFriendRequestSent, userProfile.Username), Lang.alertSuccessTitle, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void RespondToRequest(string requesterUsername, bool accepted)
        {
            if (!CanExecuteNetworkActions())
            {
                return;
            }

            try
            {
                Client.RespondToFriendRequest(SessionManager.Instance.CurrentUsername, requesterUsername, accepted);

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var requestVM = FriendRequests.FirstOrDefault(r => r.RequesterUsername == requesterUsername);
                    if (requestVM != null)
                    {
                        FriendRequests.Remove(requestVM);
                    }

                    if (accepted && !Friends.Any(f => f.Username == requesterUsername))
                    {
                        Friends.Add(new FriendViewModel { Username = requesterUsername, IsOnline = true });
                    }
                });
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async Task RemoveFriendAsync(object parameter)
        {
            if (!(parameter is FriendViewModel friend) || !CanExecuteNetworkActions()) return;

            var confirm = MessageBox.Show(
                string.Format(Lang.alertRemoveFriend, friend.Username),
                Lang.alertWarningTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    string currentUsername = SessionManager.Instance.CurrentUsername;

                    var result = await Client.RemoveFriendAsync(currentUsername, friend.Username);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (result.Success)
                        {
                            Friends.Remove(friend);
                            ShowAlert(result.Message, Lang.alertSuccessTitle, MessageBoxImage.Information);
                        }
                        else
                        {
                            ShowServiceError((AuthService.ServiceErrorType)result.ErrorCode);
                        }
                    });
                }
                catch (Exception ex)
                {
                    HandleSocialException(ex);
                }
            }
        }

        private static void ViewFriendProfile(object parameter)
        {
            if (parameter is FriendViewModel friend)
            {
                var profileVm = new FriendProfileViewModel(friend.Username);
                var view = new FriendProfileView();
                view.DataContext = profileVm;
                view.Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                view.ShowDialog();
            }
        }

        private void SubscribeToEvents()
        {
            SocialClientManager.Instance.OnFriendRequest += HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse += HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged += HandleFriendStatusChanged;
            SocialClientManager.Instance.OnFriendRemoved += HandleFriendRemoved;
        }

        private void UnsubscribeFromEvents()
        {
            SocialClientManager.Instance.OnFriendRequest -= HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse -= HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged -= HandleFriendStatusChanged;
            SocialClientManager.Instance.OnFriendRemoved -= HandleFriendRemoved;
        }

        private void HandleFriendRequest(string fromUsername)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ShowAlert(string.Format(Lang.alertFriendNewRequestFrom, fromUsername), Lang.alertFriendNewRequestTitle, MessageBoxImage.Information);

                if (!FriendRequests.Any(r => r.RequesterUsername == fromUsername))
                {
                    FriendRequests.Add(new FriendRequestViewModel(this) { RequesterUsername = fromUsername });
                }
            });
        }

        private void HandleFriendResponse(string respondingUsername, bool accepted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string message = accepted
                    ? string.Format(Lang.alertFriendRequestAccepted, respondingUsername)
                    : string.Format(Lang.alertFriendRequestDeclined, respondingUsername);

                ShowAlert(message, Lang.alertFriendRequestResponseTitle, MessageBoxImage.Information);

                if (accepted && !Friends.Any(f => f.Username == respondingUsername))
                {
                    Friends.Add(new FriendViewModel { Username = respondingUsername, IsOnline = true });
                }
            });
        }

        private void HandleFriendStatusChanged(string friendUsername, string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var friend = Friends.FirstOrDefault(f => f.Username == friendUsername);
                if (friend != null)
                {
                    friend.IsOnline = (status == "Online");
                }
            });
        }

        private void HandleFriendRemoved(string usernameWhoRemovedMe)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var friendToRemove = Friends.FirstOrDefault(f => f.Username == usernameWhoRemovedMe);

                if (friendToRemove != null)
                {
                    Friends.Remove(friendToRemove);
                    ShowAlert($"{usernameWhoRemovedMe} te ha eliminado de sus amigos.", "Aviso", MessageBoxImage.Information);
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnsubscribeFromEvents();
            }
        }

        public void Cleanup()
        {
            Dispose();
        }

        ~FriendsViewModel()
        {
            Dispose(false);
        }
    }

    public class FriendViewModel : ViewModelBase
    {
        private string _username;

        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        private bool _isOnline;

        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }
    }

    public class FriendRequestViewModel : ViewModelBase
    {
        private readonly FriendsViewModel _parent;
        public string RequesterUsername { get; set; }

        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

        public FriendRequestViewModel(FriendsViewModel parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            AcceptCommand = new RelayCommand(parameter => _parent.RespondToRequest(RequesterUsername, true));
            DeclineCommand = new RelayCommand(parameter => _parent.RespondToRequest(RequesterUsername, false));
        }
    }
}