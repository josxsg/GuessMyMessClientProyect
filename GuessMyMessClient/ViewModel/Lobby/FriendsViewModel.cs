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

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class FriendsViewModel : ViewModelBase, IDisposable
    {
        private SocialServiceClient Client => SocialClientManager.Instance.Client;

        public ObservableCollection<FriendViewModel> Friends { get; }
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }

        private ObservableCollection<UserProfileDto> _searchResults;
        public ObservableCollection<UserProfileDto> SearchResults
        {
            get { return _searchResults; }
            set
            {
                if (_searchResults != value)
                {
                    _searchResults = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
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
                LoadFriendsAndRequestsAsync();
            }
            else
            {
                Console.WriteLine("FriendsViewModel: Cliente social no listo. No se cargan datos iniciales.");
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
                Console.WriteLine($"LoadFriendsAndRequestsAsync: No se puede ejecutar. Username: '{username}', CanExecute: {CanExecuteNetworkActions()}");
                return;
            }

            try
            {
                var friends = await Client.GetFriendsListAsync(username);
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
                });

                var requests = await Client.GetFriendRequestsAsync(username);
                Application.Current.Dispatcher.Invoke(() =>
                {
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
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertFriendLoadError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error loading friends/requests: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error loading friends/requests: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertFriendLoadError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Error al cargar amigos/solicitudes: {ex.Message}");
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
                Console.WriteLine("SearchUsersAsync: No se puede ejecutar, cliente no listo.");
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
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertFriendSearchError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error searching users: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error searching users: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertFriendSearchError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Search failed: {ex.Message}");
            }
        }

        private void SendFriendRequest(object parameter)
        {
            if (parameter is UserProfileDto userProfile && CanExecuteNetworkActions())
            {
                try
                {
                    Client.SendFriendRequest(SessionManager.Instance.CurrentUsername, userProfile.Username);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SearchResults.Remove(userProfile);
                    });
                    MessageBox.Show(
                        string.Format(Lang.alertFriendRequestSent, userProfile.Username),
                        Lang.alertSuccessTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (FaultException<string> fex)
                {
                    MessageBox.Show(
                        fex.Detail,
                        Lang.alertFriendRequestErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Console.WriteLine($"Fault sending request: {fex.Detail}");
                }
                catch (CommunicationException commEx)
                {
                    MessageBox.Show(
                        Lang.alertFriendRequestSendError,
                        Lang.alertFriendRequestErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Communication Error sending request: {commEx.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertFriendRequestSendError,
                        Lang.alertFriendRequestErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Failed to send request: {ex.Message}");
                }
            }
            else if (!(parameter is UserProfileDto))
            {
                Console.WriteLine("SendFriendRequest: Parámetro inválido.");
            }
            else if (!CanExecuteNetworkActions())
            {
                Console.WriteLine("SendFriendRequest: No se puede ejecutar, cliente no listo.");
            }
        }

        public void RespondToRequest(string requesterUsername, bool accepted)
        {
            if (!CanExecuteNetworkActions())
            {
                Console.WriteLine("RespondToRequest: No se puede ejecutar, cliente no listo.");
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
                    if (accepted)
                    {
                        if (!Friends.Any(f => f.Username == requesterUsername))
                        {
                            Friends.Add(new FriendViewModel { Username = requesterUsername, IsOnline = false });
                        }
                    }
                });
            }
            catch (CommunicationException commEx)
            {
                MessageBox.Show(
                    Lang.alertFriendResponseError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Communication Error responding to request: {commEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertFriendResponseError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Failed to respond: {ex.Message}");
            }
        }

        private void InviteByEmail(object obj)
        {
        }

        private void SubscribeToEvents()
        {
            SocialClientManager.Instance.OnFriendRequest += HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse += HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged += HandleFriendStatusChanged;
            Console.WriteLine("FriendsViewModel suscrito a eventos del Manager.");
        }

        private void UnsubscribeFromEvents()
        {
            SocialClientManager.Instance.OnFriendRequest -= HandleFriendRequest;
            SocialClientManager.Instance.OnFriendResponse -= HandleFriendResponse;
            SocialClientManager.Instance.OnFriendStatusChanged -= HandleFriendStatusChanged;
            Console.WriteLine("FriendsViewModel desuscrito de eventos del Manager.");
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

                MessageBox.Show(
                    message,
                    Lang.alertFriendRequestResponseTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (accepted)
                {
                    if (!Friends.Any(f => f.Username == respondingUsername))
                    {
                        Friends.Add(new FriendViewModel { Username = respondingUsername, IsOnline = false });
                    }
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
                    bool isOnline = (status == "Online");
                    if (friend.IsOnline != isOnline)
                    {
                        friend.IsOnline = isOnline;
                        Console.WriteLine($"Estado de {friendUsername} actualizado a {status} en FriendsViewModel.");
                    }
                }
                else
                {
                    Console.WriteLine($"Cambio de estado para {friendUsername} recibido, pero no está en la lista local.");
                }
            });
        }

        ~FriendsViewModel()
        {
            Dispose(false);
        }
    }

    public class FriendViewModel : ViewModelBase
    {
        public string Username { get; set; }

        private bool _isOnline;
        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                if (_isOnline != value)
                {
                    _isOnline = value;
                    OnPropertyChanged();
                }
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
