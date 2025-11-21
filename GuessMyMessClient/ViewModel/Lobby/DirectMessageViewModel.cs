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
using ServiceSocialFault = GuessMyMessClient.SocialService.ServiceFaultDto;

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
                        Task.Run(() => LoadChatHistory(value.Username));
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
                Task.Run(() => LoadFriendsListAsync());
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
            catch (FaultException<ServiceSocialFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
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
                ClearChatHistory();
                return;
            }

            _currentChatPartnerUsername = otherUsername;
            string currentUsername = SessionManager.Instance.CurrentUsername;

            try
            {
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
                                msg.SenderUsername = Lang.alertChatSenderYou;
                            }
                            ChatHistory.Add(msg);
                        }
                    }
                });
            }
            catch (FaultException<ServiceSocialFault> fex)
            {
                ShowError(fex.Detail.Message);
                ClearChatHistory();
            }
            catch (Exception ex) when (ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
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
                    RecipientUsername = _currentChatPartnerUsername,
                    Content = MessageText,
                    Timestamp = DateTime.Now
                };

                ChatHistory.Add(localMsg);
                MessageText = string.Empty;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                ShowError(Lang.alertChatMessageSendError);
            }
            catch
            {
                ShowError(Lang.alertUnknownErrorMessage);
            }
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
            SocialClientManager.Instance.OnMessageReceived += HandleMessageReceived;
            SocialClientManager.Instance.OnFriendResponse += HandleFriendResponse;
        }

        private void UnsubscribeFromEvents()
        {
            SocialClientManager.Instance.OnMessageReceived -= HandleMessageReceived;
            SocialClientManager.Instance.OnFriendResponse -= HandleFriendResponse;
        }

        private void HandleMessageReceived(DirectMessageDto message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (message.SenderUsername == _currentChatPartnerUsername)
                {
                    ChatHistory.Add(message);
                }
            });
        }

        private void HandleFriendResponse(string fromUsername, bool accepted)
        {
            if (accepted)
            {
                Task.Run(() => LoadFriendsListAsync());
            }
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

        ~DirectMessageViewModel()
        {
            Dispose(false);
        }
    }
}
