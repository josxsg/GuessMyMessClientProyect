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
    public class DirectMessageViewModel : ViewModelBase, ISocialServiceCallback
    {
        private SocialServiceClient _client;
        // La colección 'Conversations' ahora contendrá solo Amigos.
        public ObservableCollection<FriendDto> Conversations { get; }
        public ObservableCollection<DirectMessageDto> ChatHistory { get; }
        public string MessageText { get; set; }
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

            InitializeService();
        }

        private async void InitializeService()
        {
            try
            {
                _client = new SocialServiceClient(new InstanceContext(this));
                _client.Open();
                _client.Connect(SessionManager.Instance.CurrentUsername);
                // await LoadConversationsAsync(); // MODIFICADO
                await LoadFriendsListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to chat service: {ex.Message}", "Connection Error");
                CloseClientSafely();
            }
        }

        // MODIFICADO: Nombre y lógica del método
        private async Task LoadFriendsListAsync()
        {
            if (_client?.State != CommunicationState.Opened) return;
            try
            {
                // MODIFICADO: Se llama a GetFriendsListAsync en lugar de GetConversationsAsync
                var users = await _client.GetFriendsListAsync(SessionManager.Instance.CurrentUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    foreach (var u in users) Conversations.Add(u);
                });
            }
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); CloseClientSafely(); }
            catch (Exception ex) { Console.WriteLine($"Error loading friends list: {ex.Message}"); CloseClientSafely(); } // Mensaje de error actualizado
        }

        private async void LoadChatHistory(string otherUsername)
        {
            if (string.IsNullOrEmpty(otherUsername))
            {
                ClearChatHistory();
                return;
            }

            if (_client?.State != CommunicationState.Opened) return;

            _currentChatPartnerUsername = otherUsername;
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;
                var history = await _client.GetConversationHistoryAsync(currentUsername, otherUsername);

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
                                    senderUsername = Lang.directMessageSenderTxtChat, // [You]
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
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); CloseClientSafely(); }
            catch (Exception ex) { Console.WriteLine($"Error loading chat history: {ex.Message}"); CloseClientSafely(); }
        }

        private void ClearChatHistory()
        {
            _currentChatPartnerUsername = null;
            ChatHistory.Clear();
        }

        private bool CanPerformSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && _currentChatPartnerUsername != null;
        }

        private void PerformSendMessage(object obj)
        {
            if (!CanPerformSendMessage() || _client?.State != CommunicationState.Opened) return;

            var messageDto = new DirectMessageDto
            {
                senderUsername = SessionManager.Instance.CurrentUsername,
                recipientUsername = _currentChatPartnerUsername,
                content = MessageText
            };

            try
            {
                _client.SendDirectMessage(messageDto);

                var localMsg = new DirectMessageDto { senderUsername = Lang.directMessageSenderTxtChat, content = MessageText, timestamp = DateTime.Now };
                ChatHistory.Add(localMsg);
                MessageText = string.Empty;
                OnPropertyChanged(nameof(MessageText));
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex) { MessageBox.Show($"Failed to send message: {ex.Message}"); CloseClientSafely(); }
        }

        public void Cleanup()
        {
            if (_client?.State == CommunicationState.Opened)
            {
                try
                {
                    _client.Disconnect(SessionManager.Instance.CurrentUsername);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting: {ex.Message}");
                }
            }
            CloseClientSafely();
        }

        private void CloseClientSafely()
        {
            if (_client == null) return;
            try
            {
                if (_client.State != CommunicationState.Faulted && _client.State != CommunicationState.Closed) _client.Close();
                else _client.Abort();
            }
            catch { _client.Abort(); }
            _client = null;
        }

        // --- Implementación de Callbacks ---
        public void NotifyMessageReceived(DirectMessageDto message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (message.senderUsername == _currentChatPartnerUsername)
                {
                    ChatHistory.Add(message);
                }
                else
                {
                    // Si recibes un mensaje de alguien que no es tu chat actual
                    MessageBox.Show($"Nuevo mensaje de {message.senderUsername}");

                    // MODIFICADO: Llama al nuevo método.
                    // Esto recargará la lista de amigos. Si el remitente es un amigo,
                    // aparecerá en la lista (si no estaba ya). Si no es amigo, no aparecerá.
                    LoadFriendsListAsync();
                }
            });
        }

        // NUEVO: Lógica añadida para mantener actualizada la lista de amigos del ComboBox
        public void NotifyFriendRequest(string fromUsername) { /* No es relevante aquí */ }

        public void NotifyFriendResponse(string fromUsername, bool accepted)
        {
            // Si aceptas o alguien acepta tu solicitud, recarga la lista de amigos
            if (accepted)
            {
                Application.Current.Dispatcher.Invoke(() => LoadFriendsListAsync());
            }
        }

        public void NotifyFriendStatusChanged(string friendUsername, string status)
        {
            // Si el "status" puede significar que te eliminaron (ej. "Removed" o "Unfriended")
            // es buena idea recargar la lista de amigos para que desaparezca del ComboBox.
            Application.Current.Dispatcher.Invoke(() => LoadFriendsListAsync());
        }
    }
}