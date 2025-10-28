using GuessMyMessClient.Model;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.View.Matches;
using GuessMyMessClient.View.MatchSettings;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Matches;
using GuessMyMessClient.ViewModel.MatchSettings;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class LobbyViewModel : ViewModelBase
    {
        private UserProfileDto _userProfileData;
        private string _username;
        private BitmapImage _userAvatar;
        private bool _isProfilePopupOpen;
        private ProfileViewModel _profileViewModel;
        private bool _isFriendsPopupOpen;
        private FriendsViewModel _friendsViewModel;
        private bool _isConfigurationPopupOpen;
        private ConfigurationViewModel _configurationViewModel;
        private bool _isChatPopupOpen;
        private DirectMessageViewModel _directMessageViewModel;

        public UserProfileDto UserProfileData
        {
            get { return _userProfileData; }
            set
            {
                if (_userProfileData != value)
                {
                    _userProfileData = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public BitmapImage UserAvatar
        {
            get { return _userAvatar; }
            set
            {
                if (_userAvatar != value)
                {
                    _userAvatar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsProfilePopupOpen
        {
            get { return _isProfilePopupOpen; }
            set
            {
                if (_isProfilePopupOpen != value)
                {
                    _isProfilePopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProfileViewModel ProfileViewModel
        {
            get { return _profileViewModel; }
            set
            {
                if (_profileViewModel != value)
                {
                    _profileViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFriendsPopupOpen
        {
            get { return _isFriendsPopupOpen; }
            set
            {
                if (_isFriendsPopupOpen != value)
                {
                    _isFriendsPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public FriendsViewModel FriendsViewModel
        {
            get { return _friendsViewModel; }
            set
            {
                if (_friendsViewModel != value)
                {
                    _friendsViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConfigurationPopupOpen
        {
            get { return _isConfigurationPopupOpen; }
            set
            {
                if (_isConfigurationPopupOpen != value)
                {
                    _isConfigurationPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public ConfigurationViewModel ConfigurationViewModel
        {
            get { return _configurationViewModel; }
            set
            {
                if (_configurationViewModel != value)
                {
                    _configurationViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsChatPopupOpen
        {
            get { return _isChatPopupOpen; }
            set
            {
                if (_isChatPopupOpen != value)
                {
                    _isChatPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public DirectMessageViewModel DirectMessageViewModel
        {
            get { return _directMessageViewModel; }
            set
            {
                if (_directMessageViewModel != value)
                {
                    _directMessageViewModel = value;
                    OnPropertyChanged();
                }
            }
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

            try
            {
                SocialClientManager.Instance.Initialize();
                FriendsViewModel = new FriendsViewModel();
                DirectMessageViewModel = new DirectMessageViewModel();
                ConfigurationViewModel = new ConfigurationViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Lang.alertSocialServiceInitError, ex.Message),
                    Lang.alertCriticalErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Console.WriteLine($"Error inicializando SocialClientManager: {ex.Message}");
            }

            LoadDataOnEntry();
        }

        private async void LoadDataOnEntry()
        {
            await LoadUserProfileAsync();
        }

        private void ExecuteSettings(object parameter)
        {
            if (ConfigurationViewModel != null)
            {
                IsProfilePopupOpen = false;
                IsFriendsPopupOpen = false;
                IsChatPopupOpen = false;
                IsConfigurationPopupOpen = !IsConfigurationPopupOpen;
            }
            else
            {
                MessageBox.Show(
                    Lang.alertFeatureUnavailableError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExecuteFriends(object param)
        {
            if (FriendsViewModel != null)
            {
                IsProfilePopupOpen = false;
                IsConfigurationPopupOpen = false;
                IsChatPopupOpen = false;
                IsFriendsPopupOpen = !IsFriendsPopupOpen;
            }
            else
            {
                MessageBox.Show(
                    Lang.alertFeatureUnavailableError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExecuteChat(object param)
        {
            if (DirectMessageViewModel != null)
            {
                IsProfilePopupOpen = false;
                IsConfigurationPopupOpen = false;
                IsFriendsPopupOpen = false;
                IsChatPopupOpen = !IsChatPopupOpen;
            }
            else
            {
                MessageBox.Show(
                    Lang.alertChatLoadError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExecutePlay(object param)
        {
            MatchesViewModel matchesViewModel = new MatchesViewModel();
            MatchesView matchesView = new MatchesView();
            matchesView.DataContext = matchesViewModel;
            matchesView.Show();
            CloseCurrentLobbyWindow();
        }

        private void ExecuteCreateGame(object param)
        {
            MatchSettingsViewModel matchSettingsViewModel = new MatchSettingsViewModel();
            MatchSettingsView matchSettingsView = new MatchSettingsView();
            matchSettingsView.DataContext = matchSettingsViewModel;
            matchSettingsView.Show();
            CloseCurrentLobbyWindow();
        }

        private async Task LoadUserProfileAsync()
        {
            if (!SessionManager.Instance.IsLoggedIn)
            {
                Console.WriteLine("LoadUserProfileAsync: Usuario no logueado.");
                return;
            }

            using (var client = new UserProfileServiceClient())
            {
                try
                {
                    UserProfileDto profileData = await client.GetUserProfileAsync(SessionManager.Instance.CurrentUsername);

                    if (profileData == null)
                    {
                        MessageBox.Show(
                            Lang.alertProfileLoadError,
                            Lang.alertErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    UserProfileData = profileData;
                    Username = profileData.Username;
                    ProfileViewModel = new ProfileViewModel(UserProfileData);

                    var allAvatars = await client.GetAvailableAvatarsAsync();
                    if (allAvatars != null)
                    {
                        var userAvatarDto = allAvatars.FirstOrDefault(a => a.IdAvatar == profileData.AvatarId);
                        if (userAvatarDto?.AvatarData != null)
                        {
                            UserAvatar = ConvertByteToImage(userAvatarDto.AvatarData);
                        }
                        else
                        {
                            Console.WriteLine($"Avatar con ID {profileData.AvatarId} no encontrado o sin datos.");
                        }
                    }
                }
                catch (FaultException fexGeneral)
                {
                    MessageBox.Show(
                        Lang.alertProfileLoadServerError,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"WCF Error loading profile: {fexGeneral.Message}");
                }
                catch (EndpointNotFoundException ex)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertConnectionErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Connection Error loading profile: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertProfileLoadError,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Error cargando perfil: {ex.Message}");
                }
            }
        }

        private void ExecuteEditProfile(object parameter)
        {
            if (ProfileViewModel != null)
            {
                IsConfigurationPopupOpen = false;
                IsFriendsPopupOpen = false;
                IsChatPopupOpen = false;
                IsProfilePopupOpen = !IsProfilePopupOpen;
            }
            else
            {
                MessageBox.Show(
                    Lang.alertProfileNotLoaded,
                    Lang.alertInfoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ExecuteSelectAvatar(object parameter)
        {
            if (UserProfileData == null)
            {
                MessageBox.Show(
                    Lang.alertProfileNotLoaded,
                    Lang.alertInfoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var selectAvatarView = new SelectAvatarView();
            var selectAvatarViewModel = new SelectAvatarViewModel(UserProfileData.AvatarId);
            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;
            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();
            selectAvatarViewModel.AvatarSelected -= OnAvatarSelected;
        }

        private async void OnAvatarSelected(AvatarModel newAvatar)
        {
            if (newAvatar == null || UserProfileData == null || newAvatar.Id == UserProfileData.AvatarId)
            {
                return;
            }

            UserProfileData.AvatarId = newAvatar.Id;

            using (var client = new UserProfileServiceClient())
            {
                bool success = false;
                try
                {
                    OperationResultDto result = await client.UpdateProfileAsync(Username, UserProfileData);
                    if (result.Success)
                    {
                        UserAvatar = newAvatar.ImageSource;
                        MessageBox.Show(
                            Lang.alertAvatarUpdateSuccess,
                            Lang.alertSuccessTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        success = true;
                    }
                    else
                    {
                        MessageBox.Show(
                            result.Message,
                            Lang.alertAvatarUpdateErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (FaultException fexGeneral)
                {
                    MessageBox.Show(
                        Lang.alertServerErrorMessage,
                        Lang.alertAvatarUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"WCF Error saving avatar: {fexGeneral.Message}");
                }
                catch (EndpointNotFoundException ex)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertAvatarUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Connection Error saving avatar: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertAvatarUpdateUnknownError,
                        Lang.alertAvatarUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Error guardando avatar: {ex.Message}");
                }
            }
        }

        public static BitmapImage ConvertByteToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

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
            }
            image.Freeze();
            return image;
        }

        public void CleanupSocialConnection()
        {
            FriendsViewModel?.Cleanup();
            DirectMessageViewModel?.Cleanup();
            SocialClientManager.Instance.Cleanup();
            Console.WriteLine("Conexión social limpiada.");
        }

        private void CloseCurrentLobbyWindow()
        {
            Window lobbyWindow = Application.Current.Windows.OfType<LobbyView>().FirstOrDefault();
            if (lobbyWindow != null)
            {
                lobbyWindow.Close();
            }
            else
            {
                MessageBox.Show(
                    Lang.alertLobbyWindowNotFoundError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Console.WriteLine("Error: No se pudo encontrar LobbyView para cerrar.");
            }
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                CleanupSocialConnection();
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
