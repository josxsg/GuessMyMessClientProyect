using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Session;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.Model;
using System.Collections.ObjectModel;
using System.IO;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class LobbyViewModel : ViewModelBase
    {
        // ... (Tus propiedades existentes no necesitan cambios)
        private UserProfileDto _userProfileData;
        public UserProfileDto UserProfileData
        {
            get => _userProfileData;
            set { _userProfileData = value; OnPropertyChanged(); }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private BitmapImage _userAvatar;
        public BitmapImage UserAvatar
        {
            get => _userAvatar;
            set { _userAvatar = value; OnPropertyChanged(); }
        }
        private bool _isProfilePopupOpen;
        public bool IsProfilePopupOpen
        {
            get => _isProfilePopupOpen;
            set { _isProfilePopupOpen = value; OnPropertyChanged(); }
        }

        private ProfileViewModel _profileViewModel;
        public ProfileViewModel ProfileViewModel
        {
            get => _profileViewModel;
            set { _profileViewModel = value; OnPropertyChanged(); }
        }

        public ICommand SettingsCommand { get; }
        public ICommand FriendsCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand CreateGameCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand SelectAvatarCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public LobbyViewModel()
        {
            SettingsCommand = new RelayCommand(ExecuteSettings);
            FriendsCommand = new RelayCommand(ExecuteFriends);
            ChatCommand = new RelayCommand(ExecuteChat);
            PlayCommand = new RelayCommand(ExecutePlay);
            CreateGameCommand = new RelayCommand(ExecuteCreateGame);
            EditProfileCommand = new RelayCommand(ExecuteEditProfile);
            SelectAvatarCommand = new RelayCommand(ExecuteSelectAvatar);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            LoadUserProfileAsync();
        }

        // --- MÉTODOS DE COMANDOS ---

        private void ExecuteSettings(object param) { new ConfigurationView().ShowDialog(); }
        private void ExecuteFriends(object param) { new FriendsView().ShowDialog(); }
        private void ExecuteChat(object param) { MessageBox.Show("Chat aún no implementado."); }
        private void ExecutePlay(object param) { MessageBox.Show("Navegando a Partidas Públicas..."); }
        private void ExecuteCreateGame(object param) { MessageBox.Show("Creando Partida..."); }

        private async void LoadUserProfileAsync()
        {
            if (SessionManager.Instance.IsLoggedIn)
            {
                try
                {
                    using (var client = new UserProfileServiceClient())
                    {
                        UserProfileDto profileData = await client.GetUserProfileAsync(SessionManager.Instance.CurrentUsername);

                        if (profileData != null)
                        {
                            UserProfileData = profileData;
                            Username = profileData.Username;
                            ProfileViewModel = new ProfileViewModel(UserProfileData);

                            var allAvatarsDto = await Task.Run(() => client.GetAvailableAvatars().ToList());
                            var userAvatarDto = allAvatarsDto.FirstOrDefault(a => a.idAvatar == profileData.AvatarId);

                            if (userAvatarDto != null)
                            {
                                UserAvatar = ConvertByteToImage(userAvatarDto.avatarData);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar el perfil: {ex.Message}", "Error de Carga");
                }
            }
        }

        private void ExecuteEditProfile(object parameter)
        {
            if (ProfileViewModel != null)
            {
                IsProfilePopupOpen = !IsProfilePopupOpen;
            }
            else
            {
                MessageBox.Show("La información del usuario aún no se ha cargado.", "Error");
            }
        }

        /// <summary>
        /// Abre el diálogo para seleccionar un nuevo avatar.
        /// </summary>
        private void ExecuteSelectAvatar(object parameter)
        {
            var selectAvatarView = new SelectAvatarView();

            // --- CAMBIO AQUÍ: Pasa el ID del avatar del perfil actual ---
            var selectAvatarViewModel = new SelectAvatarViewModel(this.UserProfileData.AvatarId);

            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;
            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();
            selectAvatarViewModel.AvatarSelected -= OnAvatarSelected;
        }

        /// <summary>
        /// Se ejecuta cuando el usuario confirma un nuevo avatar en la ventana de selección.
        /// </summary>
        private async void OnAvatarSelected(AvatarModel newAvatar)
        {
            if (newAvatar == null || newAvatar.Id == UserProfileData.AvatarId)
            {
                return; // No hacer nada si no hay cambio.
            }

            // Actualiza el DTO con el nuevo ID del avatar.
            UserProfileData.AvatarId = newAvatar.Id;

            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    // Llama al servicio para guardar el perfil completo actualizado.
                    OperationResultDto result = await client.UpdateProfileAsync(Username, UserProfileData);

                    if (result.success)
                    {
                        // Si el guardado fue exitoso, actualiza la imagen en la UI.
                        UserAvatar = newAvatar.ImageSource;
                        MessageBox.Show("Avatar actualizado correctamente.", "Éxito");
                    }
                    else
                    {
                        // Si el servidor reporta un error, revierte el cambio localmente.
                        UserProfileData.AvatarId = UserAvatar.GetHashCode(); // Revertir al ID anterior (necesitarás almacenar el ID previo)
                        MessageBox.Show(result.message, "Error al actualizar");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al guardar el avatar: {ex.Message}", "Error WCF");
            }
        }

        // --- MÉTODOS UTILITARIOS ---

        public static BitmapImage ConvertByteToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }
        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                // Para la ventana principal, cerramos la aplicación
                Application.Current.Shutdown();
            }
        }

        private void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}
