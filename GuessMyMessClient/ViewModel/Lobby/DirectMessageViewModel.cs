using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.Collections.ObjectModel;
using GuessMyMessClient.Properties.Langs; 
using System.ServiceModel;
using System.Linq; 
using System.Windows;
using System.Windows.Input; // Necesario para CommandManager

namespace GuessMyMessClient.ViewModel.Lobby
{
    // --- CORRECCIÓN 1: Quitada la interfaz ISocialServiceCallback ---
    public class DirectMessageViewModel : ViewModelBase, SocialService.ISocialServiceCallback
    {
        private readonly SocialServiceClient _client;
        private string _searchText;
        private string _messageText;
        private ObservableCollection<UserProfileDto> _searchResults;
        private UserProfileDto _selectedSearchResult;
        private string _currentChatPartnerUsername;

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        public string MessageText
        {
            get { return _messageText; }
            set
            {
                _messageText = value;
                OnPropertyChanged(nameof(MessageText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<UserProfileDto> SearchResults
        {
            get { return _searchResults; }
            set { _searchResults = value; OnPropertyChanged(nameof(SearchResults)); }
        }

        public UserProfileDto SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                if (_selectedSearchResult != value) // Evitar recargas innecesarias
                {
                    _selectedSearchResult = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                    if (_selectedSearchResult != null)
                    {
                        SelectedConversation = null; // Deseleccionar la otra lista
                        SearchText = string.Empty;

                        // --- MODIFICADO: Llamar a LoadChatHistory ---
                        LoadChatHistory(_selectedSearchResult.Username);
                    }
                    else if (SelectedConversation == null) // Si ambas selecciones son null
                    {
                        // --- NUEVO: Limpiar historial si no hay selección ---
                        ClearChatHistory();
                    }
                }
            }
        }

        private ObservableCollection<FriendDto> _conversations;
        public ObservableCollection<FriendDto> Conversations
        {
            get => _conversations;
            set { _conversations = value; OnPropertyChanged(); }
        }

        private FriendDto _selectedConversation;
        public FriendDto SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                if (_selectedConversation != value) // Evitar recargas innecesarias
                {
                    _selectedConversation = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                    if (_selectedConversation != null)
                    {
                        SelectedSearchResult = null; // Deseleccionar la otra lista

                        // --- MODIFICADO: Llamar a LoadChatHistory ---
                        LoadChatHistory(_selectedConversation.username);
                    }
                    else if (SelectedSearchResult == null) // Si ambas selecciones son null
                    {
                        // --- NUEVO: Limpiar historial si no hay selección ---
                        ClearChatHistory();
                    }
                }
            }
        }

        private ObservableCollection<DirectMessageDto> _chatHistory;
        public ObservableCollection<DirectMessageDto> ChatHistory
        {
            get => _chatHistory;
            set { _chatHistory = value; OnPropertyChanged(); }
        }

        public ICommand SearchCommand { get; }
        public ICommand SendMessageCommand { get; }

        public DirectMessageViewModel()
        {
            try
            {
                _client = new SocialServiceClient(new InstanceContext(this)); 
                _client.Open(); 
                SearchCommand = new RelayCommand(PerformSearch);
                SendMessageCommand = new RelayCommand(PerformSendMessage, CanPerformSendMessage);
                Conversations = new ObservableCollection<FriendDto>(); // Inicializa la nueva colección
                LoadConversations(); // Llama al método para cargar los chats
                SearchResults = new ObservableCollection<UserProfileDto>();
                ChatHistory = new ObservableCollection<DirectMessageDto>();


            }
            catch (EndpointNotFoundException epnfEx) // Usar variable epnfEx
            {
                MessageBox.Show($"Could not connect to the server: {epnfEx.Message}. Please make sure the server is running.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseClientSafely(); // Intentar cerrar si falla
            }
            catch (CommunicationException commEx) // Añadido catch específico para errores de WCF al abrir/conectar
            {
                MessageBox.Show($"Communication error connecting to the server: {commEx.Message}", "WCF Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseClientSafely();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during initialization: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseClientSafely();
            }
        }

        private async void LoadConversations()
        {
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;

                if (string.IsNullOrEmpty(currentUsername))
                {
                    Console.WriteLine("No user in session, cannot load conversations.");
                    return;
                }

                var usersWithChats = await _client.GetConversationsAsync(currentUsername);

                // Es una buena práctica actualizar la colección en el Hilo de la UI (Dispatcher)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    if (usersWithChats != null)
                    {
                        foreach (var user in usersWithChats)
                        {
                            Conversations.Add(user);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading conversations: {ex.Message}");
            }
        }

        private async void LoadChatHistory(string otherUsername)
        {
            if (string.IsNullOrEmpty(otherUsername))
            {
                ClearChatHistory(); // Limpia si no hay con quién chatear
                return;
            }

            _currentChatPartnerUsername = otherUsername; // Guardar con quién estamos hablando

            try
            {
                string currentUser = SessionManager.Instance.CurrentUsername;
                if (string.IsNullOrEmpty(currentUser)) return; // Salir si no hay sesión

                var history = await _client.GetConversationHistoryAsync(currentUser, otherUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatHistory.Clear();
                    if (history != null)
                    {
                        foreach (var msg in history)
                        {
                            // --- Lógica para el prefijo "[You]" ---
                            if (msg.senderUsername == currentUser)
                            {
                                // Usamos el recurso de idioma Lang.YouPrefix
                                msg.senderUsername = Lang.directMessageSenderTxtChat;
                            }
                            ChatHistory.Add(msg);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chat history with {otherUsername}: {ex.Message}");
                // Opcional: Mostrar error al usuario
            }
        }

        // --- NUEVO: Método para limpiar el historial ---
        private void ClearChatHistory()
        {
            _currentChatPartnerUsername = null; // Ya no hablamos con nadie
            Application.Current.Dispatcher.Invoke(() => ChatHistory.Clear());
        }
        private async void PerformSearch(object obj)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                MessageBox.Show("Cannot connect to the social service.", "Service Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
                return;
            }

            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;
                if (string.IsNullOrEmpty(currentUsername))
                {
                    MessageBox.Show("User session not found. Please log in again.", "Session Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var users = await _client.searchUsersAsync(SearchText, currentUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    if (users != null)
                    {
                        foreach (var user in users)
                        {
                            SearchResults.Add(user);
                        }
                    }
                });
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"An error occurred while searching for users: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during search: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Dispatcher.Invoke(() => SearchResults.Clear());
            }
        }

        // --- PerformSendMessage (con DirectMessageDto y llamada Async) ---
        private async void PerformSendMessage(object obj)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                MessageBox.Show("Cannot connect to the chat service.", "Service Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // El destinatario es quien esté seleccionado en CUALQUIERA de las dos listas
            string recipient = SelectedSearchResult?.Username ?? SelectedConversation?.username;
            string messageContent = MessageText;
            string sender = SessionManager.Instance.CurrentUsername;

            if (!string.IsNullOrEmpty(recipient) && !string.IsNullOrEmpty(messageContent) && !string.IsNullOrEmpty(sender))
            {
                try
                {
                    var messageDto = new SocialService.DirectMessageDto
                    {
                        senderUsername = sender,
                        recipientUsername = recipient,
                        content = messageContent,
                        // El servidor añadirá el timestamp
                    };

                    await _client.SendDirectMessageAsync(messageDto);

                    // --- NUEVO: Añadir mensaje enviado localmente ---
                    // Creamos una copia local para añadir el prefijo [You]
                    var localMessageDto = new DirectMessageDto
                    {
                        senderUsername = Lang.directMessageSenderTxtChat, // Usar prefijo
                        recipientUsername = recipient,
                        content = messageContent,
                        timestamp = DateTime.Now // Usar hora local aprox.
                    };
                    Application.Current.Dispatcher.Invoke(() => ChatHistory.Add(localMessageDto));

                    MessageText = string.Empty; // Limpiar caja de texto

                    // Si era una conversación nueva, recargar la lista de conversaciones
                    if (Conversations.FirstOrDefault(c => c.username == recipient) == null)
                    {
                        LoadConversations();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send message: {ex.Message}", "Send Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- CanPerformSendMessage (sin cambios) ---
        private bool CanPerformSendMessage(object obj)
        {
            bool aRecipientIsSelected = SelectedSearchResult != null || SelectedConversation != null;
            bool messageHasText = !string.IsNullOrWhiteSpace(MessageText);

            return aRecipientIsSelected && messageHasText;
        }

        // --- Método auxiliar CloseClientSafely (AÑADIDO para buen manejo) ---
        private void CloseClientSafely()
        {
            if (_client != null)
            {
                try
                {
                    if (_client.State != CommunicationState.Faulted && _client.State != CommunicationState.Closed)
                    {
                        _client.Close();
                    }
                    else
                    {
                        _client.Abort(); // Si está fallido o ya cerrado, abortar
                    }
                }
                catch (CommunicationObjectFaultedException ex)
                {
                    Console.WriteLine($"Client was already faulted during close: {ex.Message}");
                    _client.Abort();
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication error during client close: {ex.Message}");
                    _client.Abort();
                }
                catch (Exception ex) // Otros errores al cerrar
                {
                    Console.WriteLine($"Unexpected error during client close: {ex.Message}");
                    _client.Abort(); // Abortar si Close falla
                }
            }
        }

        // --- Método Cleanup (AÑADIDO para llamar al cerrar) ---
        // Debes llamar a este método cuando el ViewModel ya no se use (ej. al cerrar el Popup/Ventana)
        public void Cleanup()
        {
            CloseClientSafely();
        }

        public void notifyFriendRequest(string fromUsername)
        {
        }

        public void notifyFriendResponse(string fromUsername, bool accepted)
        {
        }

        public void notifyFriendStatusChanged(string friendUsername, bool isOnline)
        {
        }

        public void NotifyMessageReceived(DirectMessageDto message)
        {
        }

        // --- TODO: Añadir métodos LoadConversations y LoadConversationHistory ---
        // --- TODO: Añadir ObservableCollections para UI de conversaciones y mensajes ---

    }
}