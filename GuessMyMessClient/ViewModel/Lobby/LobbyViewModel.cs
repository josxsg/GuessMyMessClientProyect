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
using ServiceProfileFault = GuessMyMessClient.ProfileService.ServiceFaultDto;

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
            get
            {
                return _userProfileData;
            }
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
            get
            {
                return _username;
            }
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
            get
            {
                return _userAvatar;
            }
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
            get
            {
                return _isProfilePopupOpen;
            }
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
            get
            {
                return _profileViewModel;
            }
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
            get
            {
                return _isFriendsPopupOpen;
            }
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
            get
            {
                return _friendsViewModel;
            }
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
            get
            {
                return _isConfigurationPopupOpen;
            }
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
            get
            {
                return _configurationViewModel;
            }
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
            get
            {
                return _isChatPopupOpen;
            }
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
            get
            {
                return _directMessageViewModel;
            }
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

            InitializeSocialFeatures();
            Task.Run(() => LoadDataOnEntry());
        }

        private void InitializeSocialFeatures()
        {
            try
            {
                SocialClientManager.Instance.Initialize();
                FriendsViewModel = new FriendsViewModel();
                DirectMessageViewModel = new DirectMessageViewModel();
                ConfigurationViewModel = new ConfigurationViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Lang.alertSocialServiceInitError, ex.Message), Lang.alertCriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine($"Error inicializando SocialClientManager: {ex.Message}");
            }
        }

        private async Task LoadDataOnEntry()
        {
            await LoadUserProfileAsync();
        }

        private async Task LoadUserProfileAsync()
        {
            if (!SessionManager.Instance.IsLoggedIn)
            {
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                UserProfileDto profileData = await client.GetUserProfileAsync(SessionManager.Instance.CurrentUsername);

                if (profileData == null)
                {
                    ShowError(Lang.alertProfileLoadError);
                    return;
                }

                var allAvatars = await client.GetAvailableAvatarsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UserProfileData = profileData;
                    Username = profileData.Username;
                    ProfileViewModel = new ProfileViewModel(UserProfileData);

                    if (allAvatars != null)
                    {
                        var userAvatarDto = allAvatars.FirstOrDefault(a => a.IdAvatar == profileData.AvatarId);
                        if (userAvatarDto?.AvatarData != null)
                        {
                            UserAvatar = ConvertByteToImage(userAvatarDto.AvatarData);
                        }
                    }
                });

                client.Close();
                isSuccess = true;
            }
            catch (FaultException<ServiceProfileFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (FaultException)
            {
                ShowError(Lang.alertProfileLoadServerError);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
            }
            catch
            {
                ShowError(Lang.alertProfileLoadError);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private async void OnAvatarSelected(AvatarModel newAvatar)
        {
            if (newAvatar == null || UserProfileData == null || newAvatar.Id == UserProfileData.AvatarId)
            {
                return;
            }

            UserProfileData.AvatarId = newAvatar.Id;

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                OperationResultDto result = await client.UpdateProfileAsync(Username, UserProfileData);

                if (result.Success)
                {
                    UserAvatar = newAvatar.ImageSource;
                    MessageBox.Show(Lang.alertAvatarUpdateSuccess, Lang.alertSuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    MessageBox.Show(result.Message, Lang.alertAvatarUpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (FaultException<ServiceProfileFault> fex)
            {
                MessageBox.Show(fex.Detail.Message, Lang.alertAvatarUpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(Lang.alertConnectionErrorMessage, Lang.alertAvatarUpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show(Lang.alertAvatarUpdateUnknownError, Lang.alertAvatarUpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private void ExecuteSettings(object parameter)
        {
            if (ConfigurationViewModel == null)
            {
                ShowFeatureUnavailable();
                return;
            }

            CloseAllPopupsExcept("Configuration");
            IsConfigurationPopupOpen = !IsConfigurationPopupOpen;
        }

        private void ExecuteFriends(object param)
        {
            if (FriendsViewModel == null)
            {
                ShowFeatureUnavailable();
                return;
            }

            CloseAllPopupsExcept("Friends");
            IsFriendsPopupOpen = !IsFriendsPopupOpen;
        }

        private void ExecuteChat(object param)
        {
            if (DirectMessageViewModel == null)
            {
                MessageBox.Show(Lang.alertChatLoadError, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CloseAllPopupsExcept("Chat");
            IsChatPopupOpen = !IsChatPopupOpen;
        }

        private void ExecuteEditProfile(object parameter)
        {
            if (ProfileViewModel == null)
            {
                MessageBox.Show(Lang.alertProfileNotLoaded, Lang.alertInfoTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CloseAllPopupsExcept("Profile");
            IsProfilePopupOpen = !IsProfilePopupOpen;
        }

        private void CloseAllPopupsExcept(string except)
        {
            if (except != "Configuration")
            {
                IsConfigurationPopupOpen = false;
            }

            if (except != "Friends")
            {
                IsFriendsPopupOpen = false;
            }

            if (except != "Chat")
            {
                IsChatPopupOpen = false;
            }

            if (except != "Profile")
            {
                IsProfilePopupOpen = false;
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

        private void ExecuteSelectAvatar(object parameter)
        {
            if (UserProfileData == null)
            {
                MessageBox.Show(Lang.alertProfileNotLoaded, Lang.alertInfoTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectAvatarView = new SelectAvatarView();
            var selectAvatarViewModel = new SelectAvatarViewModel(UserProfileData.AvatarId);
            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;
            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();
            selectAvatarViewModel.AvatarSelected -= OnAvatarSelected;
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                CleanupSocialConnection();
                Application.Current.Shutdown();
            }
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void ShowFeatureUnavailable()
        {
            MessageBox.Show(Lang.alertFeatureUnavailableError, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
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
        }

        private static void CloseCurrentLobbyWindow()
        {
            Window lobbyWindow = Application.Current.Windows.OfType<LobbyView>().FirstOrDefault();
            if (lobbyWindow != null)
            {
                lobbyWindow.Close();
            }
            else
            {
                MessageBox.Show(Lang.alertLobbyWindowNotFoundError, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
