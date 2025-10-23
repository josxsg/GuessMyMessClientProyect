using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class FriendsViewModel : ViewModelBase, IDisposable
    {
        private SocialServiceClient Client => SocialClientManager.Instance.Client;
        public ObservableCollection<FriendViewModel> Friends { get; }
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }
        public ObservableCollection<UserProfileDto> SearchResults { get; set; }
        public string SearchText { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand InviteByEmailCommand { get; }
        public ICommand SendFriendRequestCommand { get; }

        public FriendsViewModel()
        {
            Friends = new ObservableCollection<FriendViewModel>();
            FriendRequests = new ObservableCollection<FriendRequestViewModel>();
            SearchResults = new ObservableCollection<UserProfileDto>();

            SearchCommand = new RelayCommand(async _ => await SearchUsersAsync(), _ => CanExecuteNetworkActions());
            InviteByEmailCommand = new RelayCommand(InviteByEmail, _ => CanExecuteNetworkActions());
            SendFriendRequestCommand = new RelayCommand(SendFriendRequest, _ => CanExecuteNetworkActions());

            SubscribeToEvents();

            if (CanExecuteNetworkActions())
            {
                LoadFriendsAndRequestsAsync();
            }
            else
            {
                Console.WriteLine("FriendsViewModel: El cliente social no está listo al iniciar. No se cargan datos iniciales.");
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
                Console.WriteLine($"LoadFriendsAndRequestsAsync: No se puede ejecutar. Username: {username}, CanExecute: {CanExecuteNetworkActions()}");
                return;
            }


            try
            {
                var friends = await Client.GetFriendsListAsync(username);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Friends.Clear();
                    foreach (var f in friends) Friends.Add(new FriendViewModel { Username = f.username, IsOnline = f.isOnline });
                });

                var requests = await Client.GetFriendRequestsAsync(username);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FriendRequests.Clear();
                    foreach (var r in requests) FriendRequests.Add(new FriendRequestViewModel(this) { RequesterUsername = r.requesterUsername });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar amigos/solicitudes: {ex.Message}", "Error");
            }
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
                return;
            }

            if (!CanExecuteNetworkActions()) return;

            try
            {
                var users = await Client.SearchUsersAsync(SearchText, SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    foreach (var u in users) SearchResults.Add(u);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed: {ex.Message}");
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
                    MessageBox.Show($"Friend request sent to {userProfile.Username}.", "Success");
                }
                catch (Exception ex) { MessageBox.Show($"Failed to send request: {ex.Message}"); }
            }
        }

        public void RespondToRequest(string requesterUsername, bool accepted)
        {
            if (!CanExecuteNetworkActions()) return;
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
            catch (Exception ex) { MessageBox.Show($"Failed to respond: {ex.Message}"); }
        }

        private void InviteByEmail(object obj) { }

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
                MessageBox.Show($"New friend request from {fromUsername}");
                if (!FriendRequests.Any(r => r.RequesterUsername == fromUsername))
                {
                    FriendRequests.Add(new FriendRequestViewModel(this) { RequesterUsername = fromUsername });
                }
            });
        }

        private void HandleFriendResponse(string fromUsername, bool accepted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string message = accepted ? $"{fromUsername} accepted your request." : $"{fromUsername} declined your request.";
                MessageBox.Show(message);

                if (accepted)
                {
                    if (!Friends.Any(f => f.Username == fromUsername))
                    {
                        Friends.Add(new FriendViewModel { Username = fromUsername, IsOnline = false }); 
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
                    friend.IsOnline = (status == "Online");
                    Console.WriteLine($"Estado de {friendUsername} actualizado a {status} en FriendsViewModel.");
                }
                else
                {
                    Console.WriteLine($"Se recibió cambio de estado para {friendUsername}, pero no está en la lista de amigos local.");
                }
            });
        }

    }

    public class FriendViewModel : ViewModelBase
    {
        public string Username { get; set; }
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
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
            _parent = parent;
            AcceptCommand = new RelayCommand(_ => _parent.RespondToRequest(RequesterUsername, true));
            DeclineCommand = new RelayCommand(_ => _parent.RespondToRequest(RequesterUsername, false));
        }
    }
}