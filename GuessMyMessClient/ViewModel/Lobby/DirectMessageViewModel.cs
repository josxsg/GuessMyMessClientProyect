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
        public ObservableCollection<FriendDto> Conversations { get; }
        public ObservableCollection<DirectMessageDto> ChatHistory { get; }
        public ObservableCollection<UserProfileDto> SearchResults { get; }

        public string SearchText { get; set; }
        public string MessageText { get; set; }
        private string _currentChatPartnerUsername;

        private UserProfileDto _selectedSearchResult;
        public UserProfileDto SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                _selectedSearchResult = value;
                OnPropertyChanged();
                if (value != null) { SelectedConversation = null; LoadChatHistory(value.Username); }
                else if (SelectedConversation == null) { ClearChatHistory(); } // Limpiar si ambas selecciones son null
                CommandManager.InvalidateRequerySuggested(); // Notificar cambio para CanPerformSendMessage
            }
        }

        private FriendDto _selectedConversation;
        public FriendDto SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
                if (value != null) { SelectedSearchResult = null; LoadChatHistory(value.username); }
                else if (SelectedSearchResult == null) { ClearChatHistory(); } // Limpiar si ambas selecciones son null
                CommandManager.InvalidateRequerySuggested(); // Notificar cambio para CanPerformSendMessage
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand SendMessageCommand { get; }

        public DirectMessageViewModel()
        {
            Conversations = new ObservableCollection<FriendDto>();
            ChatHistory = new ObservableCollection<DirectMessageDto>();
            SearchResults = new ObservableCollection<UserProfileDto>();

            SearchCommand = new RelayCommand(async _ => await PerformSearchAsync());
            // CORRECCIÓN: Se usa una expresión lambda para el CanExecute
            SendMessageCommand = new RelayCommand(PerformSendMessage, _ => CanPerformSendMessage());

            InitializeService();
        }

        private async void InitializeService()
        {
            try
            {
                _client = new SocialServiceClient(new InstanceContext(this));
                _client.Open(); // CORRECCIÓN: Open() es síncrono
                // Llamar a Connect para registrar el callback en el servidor
                _client.Connect(SessionManager.Instance.CurrentUsername);
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to chat service: {ex.Message}", "Connection Error");
                CloseClientSafely();
            }
        }

        private async Task LoadConversationsAsync()
        {
            if (_client?.State != CommunicationState.Opened) return; // Verificar cliente
            try
            {
                var users = await _client.GetConversationsAsync(SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    foreach (var u in users) Conversations.Add(u);
                });
            }
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); CloseClientSafely(); } // Cerrar si hay error
            catch (Exception ex) { Console.WriteLine($"Error loading conversations: {ex.Message}"); CloseClientSafely(); } // Cerrar si hay error
        }

        private async void LoadChatHistory(string otherUsername)
        {
            if (string.IsNullOrEmpty(otherUsername))
            {
                ClearChatHistory();
                return;
            }

            // Solo cargar si el cliente está abierto
            if (_client?.State != CommunicationState.Opened) return;

            _currentChatPartnerUsername = otherUsername;
            try
            {
                var history = await _client.GetConversationHistoryAsync(SessionManager.Instance.CurrentUsername, otherUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatHistory.Clear();
                    foreach (var msg in history)
                    {
                        if (msg.senderUsername == SessionManager.Instance.CurrentUsername)
                        {
                            msg.senderUsername = Lang.directMessageSenderTxtChat; // "[You]"
                        }
                        ChatHistory.Add(msg);
                    }
                });
            }
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); CloseClientSafely(); } // Cerrar si hay error
            catch (Exception ex) { Console.WriteLine($"Error loading chat history: {ex.Message}"); CloseClientSafely(); } // Cerrar si hay error
        }

        private void ClearChatHistory()
        {
            _currentChatPartnerUsername = null;
            ChatHistory.Clear();
        }

        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || _client?.State != CommunicationState.Opened) return;
            try
            {
                var users = await _client.SearchUsersAsync(SearchText, SessionManager.Instance.CurrentUsername);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    foreach (var u in users) SearchResults.Add(u);
                });
            }
            catch (FaultException ex) { MessageBox.Show(ex.Message, "Server Error"); }
            catch (Exception ex) { MessageBox.Show($"Search failed: {ex.Message}"); CloseClientSafely(); } // Cerrar si hay error
        }

        private bool CanPerformSendMessage()
        {
            // Simplificado para claridad
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
                // SendDirectMessage es OneWay, no tiene await y no necesita try-catch complicado
                _client.SendDirectMessage(messageDto);

                var localMsg = new DirectMessageDto { senderUsername = Lang.directMessageSenderTxtChat, content = MessageText, timestamp = DateTime.Now };
                ChatHistory.Add(localMsg);
                MessageText = string.Empty;
                OnPropertyChanged(nameof(MessageText)); // Notificar UI para limpiar
                CommandManager.InvalidateRequerySuggested(); // Re-evaluar CanPerformSendMessage
            }
            // Solo capturamos excepciones si SendDirectMessage (OneWay) falla, lo cual es raro
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
            _client = null; // Liberar la referencia
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
                    // Opcional: Lógica para mostrar notificación de nuevo mensaje de otro usuario
                    MessageBox.Show($"Nuevo mensaje de {message.senderUsername}");
                    LoadConversationsAsync(); // Actualizar lista si es un nuevo chat
                }
            });
        }
        public void NotifyFriendRequest(string fromUsername) { /* No es relevante aquí */ }
        public void NotifyFriendResponse(string fromUsername, bool accepted) { /* No es relevante aquí */ }
        public void NotifyFriendStatusChanged(string friendUsername, string status) { /* No es relevante aquí */ }
    }
}