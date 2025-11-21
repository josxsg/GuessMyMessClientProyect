using GuessMyMessClient.SocialService;
using System;
using System.ServiceModel;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Session
{
    public class SocialClientManager : ISocialServiceCallback
    {
        private const string SocialServiceEndpointName = "NetTcpBinding_ISocialService";

        private SocialServiceClient _client;
        private static readonly Lazy<SocialClientManager> _instance =
            new Lazy<SocialClientManager>(() => new SocialClientManager());

        public static SocialClientManager Instance => _instance.Value;

        public SocialServiceClient Client => _client;

        public event Action<string> OnFriendRequest;
        public event Action<string, bool> OnFriendResponse;
        public event Action<string, string> OnFriendStatusChanged;
        public event Action<DirectMessageDto> OnMessageReceived;
        public event Action OnConnectionLost;

        private SocialClientManager() { }

        public void Initialize()
        {
            if (IsConnected)
            {
                Console.WriteLine("SocialClientManager: Already initialized.");
                return;
            }

            try
            {
                var context = new InstanceContext(this);
                _client = new SocialServiceClient(context, SocialServiceEndpointName);
                _client.Open();

                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;

                string username = SessionManager.Instance.CurrentUsername;
                if (!string.IsNullOrEmpty(username))
                {
                    _client.Connect(username);
                    Console.WriteLine($"SocialClientManager: Connected as {username}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SocialClientManager: Error connecting: {ex.Message}");
                CleanupConnection();
            }
        }

        public bool IsConnected => _client != null && _client.State == CommunicationState.Opened;

        public void Cleanup()
        {
            string username = SessionManager.Instance.CurrentUsername;

            if (IsConnected && !string.IsNullOrEmpty(username))
            {
                try
                {
                    _client.Disconnect(username);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SocialClientManager: Error sending disconnect: {ex.Message}");
                }
            }

            CleanupConnection();
        }

        private void CleanupConnection()
        {
            if (_client != null)
            {
                try
                {
                    _client.InnerChannel.Faulted -= Channel_Faulted;
                    _client.InnerChannel.Closed -= Channel_Closed;
                }
                catch { }

                try
                {
                    if (_client.State != CommunicationState.Faulted)
                    {
                        _client.Close();
                    }
                    else
                    {
                        _client.Abort();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SocialClientManager: Error closing client: {ex.Message}");
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                }
            }
            Console.WriteLine("SocialClientManager: Connection cleaned.");
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("SocialClientManager: Channel Faulted.");
            CleanupConnection();
            OnConnectionLost?.Invoke();
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("SocialClientManager: Channel Closed.");
            if (_client != null)
            {
                CleanupConnection();
                OnConnectionLost?.Invoke();
            }
        }

        public void NotifyFriendRequest(string fromUsername)
        {
            Console.WriteLine($"Callback: Friend request from {fromUsername}");
            OnFriendRequest?.Invoke(fromUsername);
        }

        public void NotifyFriendResponse(string fromUsername, bool accepted)
        {
            Console.WriteLine($"Callback: Friend response from {fromUsername} (Accepted: {accepted})");
            OnFriendResponse?.Invoke(fromUsername, accepted);
        }

        public void NotifyFriendStatusChanged(string friendUsername, string status)
        {
            OnFriendStatusChanged?.Invoke(friendUsername, status);
        }

        public void NotifyMessageReceived(DirectMessageDto message)
        {
            if (message == null)
            {
                return;
            }
            Console.WriteLine($"Callback: DM received from {message.SenderUsername}");
            OnMessageReceived?.Invoke(message);
        }
    }
}
