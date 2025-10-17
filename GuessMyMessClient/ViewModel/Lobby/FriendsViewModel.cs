using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel.Session;
using System.Windows.Input;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class FriendsViewModel : ViewModelBase, SocialService.ISocialServiceCallback
    {
        private readonly SocialServiceClient _client;
        private string _searchText;
        private ObservableCollection<UserProfileDto> _searchResults;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FriendViewModel> Friends { get; }
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }
        public ObservableCollection<UserProfileDto> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand InviteByEmailCommand { get; }
        public ICommand SendFriendRequestCommand { get; }

        public FriendsViewModel()
        {
            try
            {
                _client = new SocialServiceClient(new InstanceContext(this));
                Friends = new ObservableCollection<FriendViewModel>();
                FriendRequests = new ObservableCollection<FriendRequestViewModel>();
                SearchResults = new ObservableCollection<UserProfileDto>();

                SearchCommand = new RelayCommand(SearchUsers);
                InviteByEmailCommand = new RelayCommand(InviteByEmail);
                SendFriendRequestCommand = new RelayCommand(SendFriendRequest);

                LoadFriendsAndRequests();
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show("Could not connect to the server. Please make sure the server is running.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadFriendsAndRequests()
        {
            string username = SessionManager.Instance.CurrentUsername;
            if (string.IsNullOrEmpty(username)) return;

            try
            {
                var friends = await _client.getFriendsListAsync(username);
                Friends.Clear();
                foreach (var friend in friends)
                {
                    Friends.Add(new FriendViewModel
                    {
                        Username = friend.username,
                        IsOnline = friend.isOnline
                    });
                }

                var requests = await _client.getFriendRequestsAsync(username);
                FriendRequests.Clear();
                foreach (var request in requests)
                {
                    FriendRequests.Add(new FriendRequestViewModel(_client)
                    {
                        RequesterUsername = request.requesterUsername
                    });
                }
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"An error occurred while communicating with the server: {ex.Message}", "Communication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SearchUsers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
                return;
            }

            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;

                var users = await _client.searchUsersAsync(SearchText, currentUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    foreach (var user in users)
                    {
                        SearchResults.Add(user);
                    }
                });
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"An error occurred while searching for users: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SendFriendRequest(object targetUsername)
        {
            if (targetUsername is string username)
            {
                string requesterUsername = SessionManager.Instance.CurrentUsername;
                try
                {
                    await _client.sendFriendRequestAsync(requesterUsername, username);
                    MessageBox.Show($"Friend request sent to {username}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (CommunicationException ex)
                {
                    MessageBox.Show($"Failed to send friend request: {ex.Message}", "Request Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InviteByEmail(object obj)
        {
            MessageBox.Show("Invite by email functionality is not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public void notifyFriendRequest(string fromUsername)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!FriendRequests.Any(fr => fr.RequesterUsername == fromUsername))
                {
                    FriendRequests.Add(new FriendRequestViewModel(_client) { RequesterUsername = fromUsername });
                    MessageBox.Show($"You have a new friend request from {fromUsername}!", "New Friend Request", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }
        public void notifyFriendResponse(string fromUsername, bool accepted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string message = accepted
                    ? $"{fromUsername} accepted your friend request!"
                    : $"{fromUsername} declined your friend request.";

                MessageBox.Show(message, "Friend Request Response", MessageBoxButton.OK, MessageBoxImage.Information);

                if (accepted)
                {
                    LoadFriendsAndRequests();
                }
            });
        }
        public void notifyFriendStatusChanged(string friendUsername, bool isOnline)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var friend = Friends.FirstOrDefault(f => f.Username == friendUsername);
                if (friend != null)
                {
                    friend.IsOnline = isOnline;
                }
            });
        }
    }

    public class FriendViewModel : ViewModelBase
    {
        public string Username { get; set; }
        public bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }
    }

    public class FriendRequestViewModel : ViewModelBase
    {
        private readonly SocialServiceClient _client;
        public string RequesterUsername { get; set; }
        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }

        public FriendRequestViewModel(SocialServiceClient client)
        {
            _client = client;
            AcceptCommand = new RelayCommand(p => RespondToRequest(true));
            DeclineCommand = new RelayCommand(p => RespondToRequest(false));
        }

        private async void RespondToRequest(bool accepted)
        {
            string targetUsername = SessionManager.Instance.CurrentUsername;
            try
            {
                await _client.respondToFriendRequestAsync(targetUsername, RequesterUsername, accepted);
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"Failed to respond to the request: {ex.Message}", "Response Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
