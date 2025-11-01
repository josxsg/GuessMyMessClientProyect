using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.ViewModel;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class DirectMessageViewModel : ViewModelBase, IDisposable
    {
        private static SocialServiceClient Client => SocialClientManager.Instance.Client;

        public ObservableCollection<FriendDto> Conversations { get; }
        public ObservableCollection<DirectMessageDto> ChatHistory { get; }
        private string _messageText;
        public string MessageText
        {
            get
            {
                return _messageText;
            }
            set
            {
                if (_messageText != value)
                {
                    _messageText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _currentChatPartnerUsername;
        private FriendDto _selectedConversation;
        public FriendDto SelectedConversation
        {
            get
            {
                return _selectedConversation;
            }
            set
            {
                if (_selectedConversation != value)
                {
                    _selectedConversation = value;
                    OnPropertyChanged();

                    if (value != null)
                    {
                        LoadChatHistory(value.Username);
                    }
                    else
                    {
                        ClearChatHistory();
                    }
                }
            }
        }

        public ICommand SendMessageCommand { get; }

        public DirectMessageViewModel()
        {
            Conversations = new ObservableCollection<FriendDto>();
            ChatHistory = new ObservableCollection<DirectMessageDto>();
            SendMessageCommand = new RelayCommand(PerformSendMessage, parameter => CanPerformSendMessage());

            SubscribeToEvents();

            if (CanExecuteNetworkActions())
            {
                LoadFriendsListAsync();
            }
            else
            {
                Console.WriteLine("DirectMessageViewModel: Cliente social no listo al iniciar.");
            }
        }

        private bool CanExecuteNetworkActions()
        {
            return Client != null && Client.State == CommunicationState.Opened;
        }

        private async Task LoadFriendsListAsync()
        {
            if (!CanExecuteNetworkActions())
            {
                Console.WriteLine("LoadFriendsListAsync: No se puede ejecutar, cliente no listo.");
                return;
            }
            try
            {
                var users = await Client.GetFriendsListAsync(SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    if (users != null)
                    {
                        foreach (var u in users)
                        {
                            Conversations.Add(u);
                        }
                    }
                });
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertChatLoadFriendsError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error loading friends list: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error loading friends list: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertChatLoadFriendsError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Error loading friends list in DVM: {ex.Message}");
            }
        }

        private async Task LoadChatHistory(string otherUsername)
        {
            if (string.IsNullOrEmpty(otherUsername))
            {
                ClearChatHistory();
                return;
            }

            if (!CanExecuteNetworkActions())
            {
                Console.WriteLine("LoadChatHistory: No se puede ejecutar, cliente no listo.");
                ClearChatHistory();
                return;
            }

            _currentChatPartnerUsername = otherUsername;
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;
                var history = await Client.GetConversationHistoryAsync(currentUsername, otherUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatHistory.Clear();
                    if (history != null)
                    {
                        foreach (var msg in history)
                        {
                            if (msg.SenderUsername == currentUsername)
                            {
                                var localMessageDto = new DirectMessageDto
                                {
                                    SenderUsername = Lang.alertChatSenderYou,
                                    RecipientUsername = msg.RecipientUsername,
                                    Content = msg.Content,
                                    Timestamp = msg.Timestamp
                                };
                                ChatHistory.Add(localMessageDto);
                            }
                            else
                            {
                                ChatHistory.Add(msg);
                            }
                        }
                    }
                });
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertChatLoadHistoryError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error loading chat history: {fexGeneral.Message}");
                ClearChatHistory();
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error loading chat history: {ex.Message}");
                ClearChatHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertChatLoadHistoryError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Error loading chat history: {ex.Message}");
                ClearChatHistory();
            }
        }

        private void ClearChatHistory()
        {
            _currentChatPartnerUsername = null;
            Application.Current.Dispatcher.Invoke(() => ChatHistory.Clear());
        }

        private bool CanPerformSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) &&
                   _currentChatPartnerUsername != null &&
                   CanExecuteNetworkActions();
        }

        private void PerformSendMessage(object obj)
        {
            if (!CanPerformSendMessage())
            {
                return;
            }

            var messageDto = new DirectMessageDto
            {
                SenderUsername = SessionManager.Instance.CurrentUsername,
                RecipientUsername = _currentChatPartnerUsername,
                Content = MessageText,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                Client.SendDirectMessage(messageDto);

                var localMsg = new DirectMessageDto
                {
                    SenderUsername = Lang.alertChatSenderYou,
                    Content = MessageText,
                    Timestamp = DateTime.Now
                };
                ChatHistory.Add(localMsg);
                MessageText = string.Empty;
            }
            catch (CommunicationException commEx)
            {
                MessageBox.Show(
                    Lang.alertChatMessageSendError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Communication Error sending message: {commEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertChatMessageSendError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Failed to send message: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            SocialClientManager.Instance.OnMessageReceived += HandleMessageReceived;
            SocialClientManager.Instance.OnFriendResponse += HandleFriendResponse;
            Console.WriteLine("DirectMessageViewModel suscrito a eventos del Manager.");
        }

        private void UnsubscribeFromEvents()
        {
            SocialClientManager.Instance.OnMessageReceived -= HandleMessageReceived;
            SocialClientManager.Instance.OnFriendResponse -= HandleFriendResponse;
            Console.WriteLine("DirectMessageViewModel desuscrito de eventos del Manager.");
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

        private void HandleMessageReceived(DirectMessageDto message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (message.SenderUsername == _currentChatPartnerUsername)
                {
                    ChatHistory.Add(message);
                }
                else
                {
                    MessageBox.Show(
                        string.Format(Lang.alertChatNewMessageFrom, message.SenderUsername),
                        Lang.alertChatNewMessageTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            });
        }

        private void HandleFriendResponse(string fromUsername, bool accepted)
        {
            if (accepted)
            {
                Application.Current.Dispatcher.Invoke(() => LoadFriendsListAsync());
            }
        }

        ~DirectMessageViewModel()
        {
            Dispose(false);
        }
    }
}
