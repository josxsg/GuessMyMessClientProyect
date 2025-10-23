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

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class DirectMessageViewModel : ViewModelBase, IDisposable
    {
        private SocialServiceClient Client => SocialClientManager.Instance.Client;

        public ObservableCollection<FriendDto> Conversations { get; }
        public ObservableCollection<DirectMessageDto> ChatHistory { get; }

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _currentChatPartnerUsername;
        private FriendDto _selectedConversation;
        public FriendDto SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();

                if (value != null)
                {
                    LoadChatHistory(value.username);
                }
                else
                {
                    ClearChatHistory();
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public ICommand SendMessageCommand { get; }

        public DirectMessageViewModel()
        {
            Conversations = new ObservableCollection<FriendDto>();
            ChatHistory = new ObservableCollection<DirectMessageDto>();
            SendMessageCommand = new RelayCommand(PerformSendMessage, _ => CanPerformSendMessage()); 

            SubscribeToEvents();

            if (CanExecuteNetworkActions())
            {
                LoadFriendsListAsync(); 
            }
            else
            {
                Console.WriteLine("DirectMessageViewModel: El cliente social no está listo al iniciar.");
            }
        }

        private bool CanExecuteNetworkActions()
        {
            return Client != null && Client.State == CommunicationState.Opened;
        }


        private async Task LoadFriendsListAsync()
        {
            if (!CanExecuteNetworkActions()) return;
            try
            {
                var users = await Client.GetFriendsListAsync(SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    foreach (var u in users) Conversations.Add(u);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading friends list in DVM: {ex.Message}");
                MessageBox.Show($"Error al cargar la lista de amigos para el chat: {ex.Message}", "Error");
            }
        }

        private async void LoadChatHistory(string otherUsername)
        {
            if (string.IsNullOrEmpty(otherUsername))
            {
                ClearChatHistory(); 
                return;
            }

            if (!CanExecuteNetworkActions()) return;

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
                            if (msg.senderUsername == currentUsername)
                            {
                                var localMessageDto = new DirectMessageDto
                                {
                                    senderUsername = Lang.directMessageSenderTxtChat, 
                                    recipientUsername = msg.recipientUsername,
                                    content = msg.content,
                                    timestamp = msg.timestamp
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chat history: {ex.Message}");
                MessageBox.Show($"Error al cargar el historial del chat: {ex.Message}", "Error");
            }
        }

        private void ClearChatHistory()
        {
            _currentChatPartnerUsername = null;
            Application.Current.Dispatcher.Invoke(() => ChatHistory.Clear());
            OnPropertyChanged(nameof(ChatHistory)); 
            CommandManager.InvalidateRequerySuggested(); 
        }

        private bool CanPerformSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) &&
                   _currentChatPartnerUsername != null &&
                   CanExecuteNetworkActions(); 
        }

        private void PerformSendMessage(object obj) 
        {
            if (!CanPerformSendMessage()) return; 

            var messageDto = new DirectMessageDto
            {
                senderUsername = SessionManager.Instance.CurrentUsername,
                recipientUsername = _currentChatPartnerUsername,
                content = MessageText
            };

            try
            {
                Client.SendDirectMessage(messageDto);

                var localMsg = new DirectMessageDto { senderUsername = Lang.directMessageSenderTxtChat, content = MessageText, timestamp = DateTime.Now };
                ChatHistory.Add(localMsg); 
                MessageText = string.Empty; 
            }
            catch (Exception ex) { MessageBox.Show($"Failed to send message: {ex.Message}"); }
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
                if (message.senderUsername == _currentChatPartnerUsername)
                {
                    ChatHistory.Add(message);
                }
                else
                {
                    MessageBox.Show($"Nuevo mensaje de {message.senderUsername}");
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

    }
}