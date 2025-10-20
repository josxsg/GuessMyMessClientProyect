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
using GuessMyMessClient.ViewModel.Lobby;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class LobbyViewModel : ViewModelBase
    {
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

        private bool _isFriendsPopupOpen;
        public bool IsFriendsPopupOpen
        {
            get => _isFriendsPopupOpen;
            set { _isFriendsPopupOpen = value; OnPropertyChanged(); }
        }

        private FriendsViewModel _friendsViewModel;
        public FriendsViewModel FriendsViewModel
        {
            get => _friendsViewModel;
            set { _friendsViewModel = value; OnPropertyChanged(); }
        }

        private bool _isConfigurationPopupOpen;
        public bool IsConfigurationPopupOpen
        {
            get => _isConfigurationPopupOpen;
            set { _isConfigurationPopupOpen = value; OnPropertyChanged(); }
        }

        private ConfigurationViewModel _configurationViewModel;
        public ConfigurationViewModel ConfigurationViewModel
        {
            get => _configurationViewModel;
            set { _configurationViewModel = value; OnPropertyChanged(); }
        }

        private bool _isChatPopupOpen;
        public bool IsChatPopupOpen
        {
            get => _isChatPopupOpen;
            set { _isChatPopupOpen = value; OnPropertyChanged(); }
        }

        private DirectMessageViewModel _directMessageViewModel;
        public DirectMessageViewModel DirectMessageViewModel
        {
            get => _directMessageViewModel;
            set { _directMessageViewModel = value; OnPropertyChanged(); }
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

            FriendsViewModel = new FriendsViewModel();
            ConfigurationViewModel = new ConfigurationViewModel();
            DirectMessageViewModel = new DirectMessageViewModel();

            LoadUserProfileAsync();
        }


        private void ExecuteSettings(object parameter)
        {
            if (ConfigurationViewModel != null)
            {
                IsConfigurationPopupOpen = !IsConfigurationPopupOpen;
            }
            else
            {
                MessageBox.Show("La información del usuario aún no se ha cargado.", "Error");
            }
        }

        private void ExecuteFriends(object param)
        {
            if (FriendsViewModel != null)
            {
                IsFriendsPopupOpen = !IsFriendsPopupOpen;
            }
            else
            {
                MessageBox.Show("La información del usuario aún no se ha cargado.", "Error");
            }
        }
        private void ExecuteChat(object param)
        {
            if (DirectMessageViewModel != null)
            {
                IsChatPopupOpen = !IsChatPopupOpen;
            }
            else
            {
                MessageBox.Show("No se puede cargar el chat.", "Error");
            }
        }
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

        private void ExecuteSelectAvatar(object parameter)
        {
            var selectAvatarView = new SelectAvatarView();

            var selectAvatarViewModel = new SelectAvatarViewModel(this.UserProfileData.AvatarId);

            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;
            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();
            selectAvatarViewModel.AvatarSelected -= OnAvatarSelected;
        }

        private async void OnAvatarSelected(AvatarModel newAvatar)
        {
            if (newAvatar == null || newAvatar.Id == UserProfileData.AvatarId)
            {
                return; 
            }

            UserProfileData.AvatarId = newAvatar.Id;

            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    OperationResultDto result = await client.UpdateProfileAsync(Username, UserProfileData);

                    if (result.success)
                    {
                        UserAvatar = newAvatar.ImageSource;
                        MessageBox.Show("Avatar actualizado correctamente.", "Éxito");
                    }
                    else
                    {
                        UserProfileData.AvatarId = UserAvatar.GetHashCode(); 
                        MessageBox.Show(result.message, "Error al actualizar");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al guardar el avatar: {ex.Message}", "Error WCF");
            }
        }


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
