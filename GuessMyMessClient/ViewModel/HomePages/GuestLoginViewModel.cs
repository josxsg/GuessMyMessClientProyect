using GuessMyMessClient.AuthService;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.View.Lobby;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.ViewModel.Lobby;
using System.Linq;
using System;
using GuessMyMessClient.View.WaitingRoom;
using GuessMyMessClient.ViewModel.WaitingRoom;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class GuestLoginViewModel : ViewModelBase
    {
        private string _email;
        private string _invitationCode;

        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string InvitationCode { get => _invitationCode; set { _invitationCode = value; OnPropertyChanged(); } }

        public ICommand LoginGuestCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand BackCommand { get; }

        public GuestLoginViewModel()
        {
            LoginGuestCommand = new RelayCommand(ExecuteLoginGuest);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        private async void ExecuteLoginGuest(object obj)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(InvitationCode))
            {
                MessageBox.Show("Todos los campos son obligatorios.");
                return;
            }

            var client = new AuthenticationServiceClient();
            try
            {
                var result = await client.LoginAsGuestAsync(Email, InvitationCode);

                if (result.Success)
                {
                    // 1. Configurar Sesión
                    string sessionToken = result.Message;
                    string matchId = result.Data["MatchId"];
                    bool isPrivate = bool.Parse(result.Data["IsPrivate"]);

                    SessionManager.Instance.StartSession(sessionToken);
                    SessionManager.Instance.IsGuest = true;

                    // 2. Conectar al Lobby (Servidor)
                    // Esto registra al jugador en la partida en el backend
                    GuessMyMessClient.ViewModel.Session.LobbyClientManager.Instance.Connect(sessionToken, matchId);

                    // 3. Abrir la Ventana Correcta (Waiting Room)
                    Window waitingRoomWindow;

                    if (isPrivate)
                    {
                        // --- CORRECCIÓN AQUÍ: Pasamos los argumentos requeridos ---
                        var vm = new WaitingRoomPrivateMatchViewModel(
                            GuessMyMessClient.ViewModel.Session.LobbyClientManager.Instance,
                            SessionManager.Instance);
                        waitingRoomWindow = new WaitingRoomPrivateMatchView { DataContext = vm };
                    }
                    else
                    {
                        // --- CORRECCIÓN AQUÍ: Pasamos los argumentos requeridos ---
                        var vm = new WaitingRoomPublicMatchViewModel(
                            GuessMyMessClient.ViewModel.Session.LobbyClientManager.Instance,
                            SessionManager.Instance);
                        waitingRoomWindow = new WaitingRoomPublicMatchView { DataContext = vm };
                    }

                    waitingRoomWindow.Show();

                    // 4. Cerrar Ventana Actual
                    Application.Current.Windows.OfType<GuestLoginView>().FirstOrDefault()?.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al ingresar: " + ex.Message);
            }
            finally
            {
                try { client.Close(); } catch { client.Abort(); }
            }
        }

        private static void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                Application.Current.Shutdown();
            }
        }

        private static void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private static void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void ExecuteBack(object obj)
        {
            // Regresar a MainView
            new MainView().Show();
            Application.Current.Windows.OfType<GuestLoginView>().FirstOrDefault()?.Close();
        }
    }
}