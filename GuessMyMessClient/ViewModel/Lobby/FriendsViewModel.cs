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
    // CORRECCIÓN: Se implementa la interfaz de callback completamente
    public class FriendsViewModel : ViewModelBase, ISocialServiceCallback
    {
        private SocialServiceClient _client;
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

            SearchCommand = new RelayCommand(async _ => await SearchUsersAsync());
            InviteByEmailCommand = new RelayCommand(InviteByEmail);
            SendFriendRequestCommand = new RelayCommand(SendFriendRequest);

            InitializeService();
        }

        private async void InitializeService()
        {
            try
            {
                _client = new SocialServiceClient(new InstanceContext(this));
                _client.Open(); // CORRECCIÓN: Open() es síncrono
                await LoadFriendsAndRequestsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to social service: {ex.Message}", "Connection Error");
                CloseClientSafely();
            }
        }

        private async Task LoadFriendsAndRequestsAsync()
        {
            string username = SessionManager.Instance.CurrentUsername;
            if (string.IsNullOrEmpty(username) || _client.State != CommunicationState.Opened) return;

            try
            {
                // CORRECCIÓN: Usa el nombre de método ...Async de la nueva interfaz
                var friends = await _client.GetFriendsListAsync(username);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Friends.Clear();
                    foreach (var f in friends) Friends.Add(new FriendViewModel { Username = f.username, IsOnline = f.isOnline });
                });

                var requests = await _client.GetFriendRequestsAsync(username);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FriendRequests.Clear();
                    foreach (var r in requests) FriendRequests.Add(new FriendRequestViewModel(this) { RequesterUsername = r.requesterUsername });
                });
            }
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Communication Error"); CloseClientSafely(); }
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || _client.State != CommunicationState.Opened) return;
            try
            {
                var users = await _client.SearchUsersAsync(SearchText, SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    foreach (var u in users) SearchResults.Add(u);
                });
            }
            catch (Exception ex) { MessageBox.Show($"Search failed: {ex.Message}"); }
        }

        private void SendFriendRequest(object targetUsername)
        {
            if (targetUsername is string username && _client.State == CommunicationState.Opened)
            {
                try
                {
                    // Es OneWay, no necesita await
                    _client.SendFriendRequest(SessionManager.Instance.CurrentUsername, username);
                    MessageBox.Show($"Friend request sent to {username}.", "Success");
                }
                catch (Exception ex) { MessageBox.Show($"Failed to send request: {ex.Message}"); }
            }
        }

        public async Task RespondToRequestAsync(string requesterUsername, bool accepted)
        {
            if (_client.State != CommunicationState.Opened) return;
            try
            {
                // Es OneWay, no necesita await
                _client.RespondToFriendRequest(SessionManager.Instance.CurrentUsername, requesterUsername, accepted);
                await LoadFriendsAndRequestsAsync(); // Recargar listas
            }
            catch (Exception ex) { MessageBox.Show($"Failed to respond: {ex.Message}"); }
        }

        private void InviteByEmail(object obj) { /* Lógica de invitación */ }
        public void Cleanup() => CloseClientSafely();
        private void CloseClientSafely()
        {
            if (_client == null) return;
            try
            {
                if (_client.State != CommunicationState.Faulted) _client.Close();
                else _client.Abort();
            }
            catch { _client.Abort(); }
        }

        // --- Implementación COMPLETA de Callbacks ---
        public void NotifyFriendRequest(string fromUsername)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                MessageBox.Show($"New friend request from {fromUsername}");
                await LoadFriendsAndRequestsAsync();
            });
        }
        public void NotifyFriendResponse(string fromUsername, bool accepted)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                string message = accepted ? $"{fromUsername} accepted your request." : $"{fromUsername} declined your request.";
                MessageBox.Show(message);
                await LoadFriendsAndRequestsAsync();
            });
        }
        public void NotifyFriendStatusChanged(string friendUsername, string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var friend = Friends.FirstOrDefault(f => f.Username == friendUsername);
                if (friend != null) friend.IsOnline = (status == "Online");
            });
        }
        public void NotifyMessageReceived(DirectMessageDto message) { /* Lógica para notificar mensaje */ }
    }

    // CORRECCIÓN: Se añaden las propiedades que faltaban
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
        // CORRECCIÓN: Pasa el FriendsViewModel (parent) en lugar del client
        private readonly FriendsViewModel _parent;
        public string RequesterUsername { get; set; }
        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

        public FriendRequestViewModel(FriendsViewModel parent)
        {
            _parent = parent;
            AcceptCommand = new RelayCommand(async _ => await _parent.RespondToRequestAsync(RequesterUsername, true));
            DeclineCommand = new RelayCommand(async _ => await _parent.RespondToRequestAsync(RequesterUsername, false));
        }
    }
}