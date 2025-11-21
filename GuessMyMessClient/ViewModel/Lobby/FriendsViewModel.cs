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
using GuessMyMessClient.ViewModel;
using ServiceSocialFault = GuessMyMessClient.SocialService.ServiceFaultDto;

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
        public ICommand InviteByEmailCommand { get; }
        public ICommand SendFriendRequestCommand { get; }

        public FriendsViewModel()
        {
            Friends = new ObservableCollection<FriendViewModel>();
            FriendRequests = new ObservableCollection<FriendRequestViewModel>();
            SearchResults = new ObservableCollection<UserProfileDto>();

            SearchCommand = new RelayCommand(async parameter => await SearchUsersAsync(), parameter => CanExecuteNetworkActions());
            InviteByEmailCommand = new RelayCommand(InviteByEmail, parameter => CanExecuteNetworkActions());
            SendFriendRequestCommand = new RelayCommand(SendFriendRequest, parameter => CanExecuteNetworkActions());

            SubscribeToEvents();

            if (CanExecuteNetworkActions())
            {
                Task.Run(() => LoadFriendsAndRequestsAsync());
            }
        }

        private bool CanExecuteNetworkActions()
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Friends.Clear();
                    if (friends != null)
                    {
                        foreach (var f in friends)
                        {
                            Friends.Add(new FriendViewModel { Username = f.Username, IsOnline = f.IsOnline });
                        }
                    }

                    FriendRequests.Clear();
                    if (requests != null)
                    {
                        foreach (var r in requests)
                        {
                            FriendRequests.Add(new FriendRequestViewModel(this) { RequesterUsername = r.RequesterUsername });
                        }
                    }
                });
            }
            catch (FaultException<ServiceSocialFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
            }
            catch
            {
                ShowError(Lang.alertFriendLoadError);
            }
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
                return;
            }

            if (!CanExecuteNetworkActions())
            {
                return;
            }

            try
            {
                var users = await Client.SearchUsersAsync(SearchText, SessionManager.Instance.CurrentUsername);

                Application.Current.Dispatcher.Invoke(() =>
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
            catch (FaultException<ServiceSocialFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
            }
            catch
            {
                ShowError(Lang.alertFriendSearchError);
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Remove(userProfile);
                    MessageBox.Show(
                        string.Format(Lang.alertFriendRequestSent, userProfile.Username),
                        Lang.alertSuccessTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                ShowError(Lang.alertFriendRequestSendError);
            }
            catch
            {
                ShowError(Lang.alertUnknownErrorMessage);
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

                Application.Current.Dispatcher.Invoke(() =>
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
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                ShowError(Lang.alertFriendResponseError);
            }
            catch
            {
                ShowError(Lang.alertUnknownErrorMessage);
            }
        }

        private void InviteByEmail(object obj)
        {
            MessageBox.Show("Funcionalidad de invitación por correo no implementada.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void SubscribeToEvents()
        {
            SocialClientManager.Instance.OnFriendRequest += HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse += HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged += HandleFriendStatusChanged;
        }

        private void UnsubscribeFromEvents()
        {
            SocialClientManager.Instance.OnFriendRequest -= HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse -= HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged -= HandleFriendStatusChanged;
        }

        private void HandleFriendRequest(string fromUsername)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    string.Format(Lang.alertFriendNewRequestFrom, fromUsername),
                    Lang.alertFriendNewRequestTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

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

                MessageBox.Show(message, Lang.alertFriendRequestResponseTitle, MessageBoxButton.OK, MessageBoxImage.Information);

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
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

        private bool _isOnline;
        public bool IsOnline { get => _isOnline; set { _isOnline = value; OnPropertyChanged(); } }
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
