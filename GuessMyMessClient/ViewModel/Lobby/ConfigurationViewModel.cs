using System;
using System.Linq;
using System.Windows.Input;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using System.Windows;
using GuessMyMessClient.ViewModel;
using System.ServiceModel;
using System.Globalization;
using System.Threading;
using GuessMyMessClient.Properties.Langs; 

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class ConfigurationViewModel : ViewModelBase
    {
        public ICommand LogOutCommand { get; }

        private bool _isSpanish;
        private bool _isEnglish;
        private string _configTitle;
        private string _soundTitle;
        private string _soundEffectsLabel;
        private string _musicLabel;
        private string _volumeLabel;
        private string _languageTitle;
        private string _spanishButtonText;
        private string _englishButtonText;
        private string _logoutButtonText;

        public string ConfigTitle
        {
            get => _configTitle;
            set { _configTitle = value; OnPropertyChanged(nameof(ConfigTitle)); }
        }
        public string SoundTitle
        {
            get => _soundTitle;
            set { _soundTitle = value; OnPropertyChanged(nameof(SoundTitle)); }
        }
        public string SoundEffectsLabel
        {
            get => _soundEffectsLabel;
            set { _soundEffectsLabel = value; OnPropertyChanged(nameof(SoundEffectsLabel)); }
        }
        public string MusicLabel
        {
            get => _musicLabel;
            set { _musicLabel = value; OnPropertyChanged(nameof(MusicLabel)); }
        }
        public string VolumeLabel
        {
            get => _volumeLabel;
            set { _volumeLabel = value; OnPropertyChanged(nameof(VolumeLabel)); }
        }
        public string LanguageTitle
        {
            get => _languageTitle;
            set { _languageTitle = value; OnPropertyChanged(nameof(LanguageTitle)); }
        }
        public string SpanishButtonText
        {
            get => _spanishButtonText;
            set { _spanishButtonText = value; OnPropertyChanged(nameof(SpanishButtonText)); }
        }
        public string EnglishButtonText
        {
            get => _englishButtonText;
            set { _englishButtonText = value; OnPropertyChanged(nameof(EnglishButtonText)); }
        }
        public string LogoutButtonText
        {
            get => _logoutButtonText;
            set { _logoutButtonText = value; OnPropertyChanged(nameof(LogoutButtonText)); }
        }

        public ConfigurationViewModel()
        {
            LogOutCommand = new RelayCommand(ExecuteLogout);

            string currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            if (currentCulture.StartsWith("es"))
            {
                _isSpanish = true;
            }
            else
            {
                _isEnglish = true;
            }
            UpdateLanguageProperties();
        }

        public bool IsSpanish
        {
            get => _isSpanish;
            set
            {
                if (value && !_isSpanish)
                {
                    _isSpanish = true;
                    _isEnglish = false; 

                    ChangeLanguage("es-MX");
                    OnPropertyChanged(nameof(IsSpanish));
                    OnPropertyChanged(nameof(IsEnglish));
                }
            }
        }

        public bool IsEnglish
        {
            get => _isEnglish;
            set
            {
                if (value && !_isEnglish)
                {
                    _isEnglish = true;
                    _isSpanish = false; 

                    ChangeLanguage("en-US");
                    OnPropertyChanged(nameof(IsEnglish));
                    OnPropertyChanged(nameof(IsSpanish));
                }
            }
        }

        private void ChangeLanguage(string cultureName)
        {
            try
            {
                CultureInfo newCulture = new CultureInfo(cultureName);

                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
                Lang.Culture = newCulture;

                Window currentLobbyWindow = Application.Current.Windows.OfType<LobbyView>().FirstOrDefault();

                var newLobbyView = new LobbyView();
                newLobbyView.Show();

                if (currentLobbyWindow != null)
                {
                    currentLobbyWindow.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar el idioma: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLanguageProperties()
        {
            ConfigTitle = Lang.configLbConfiguration;
            SoundTitle = Lang.configLbSound;
            SoundEffectsLabel = Lang.configLbSoundEffects;
            MusicLabel = Lang.configLbMusic;
            VolumeLabel = Lang.configLbVolume;
            LanguageTitle = Lang.configLbLanguage;
            SpanishButtonText = Lang.configBtnSpanish;
            EnglishButtonText = Lang.configBtnEnglish;
            LogoutButtonText = Lang.configtBtnLogOut;
        }

        private void ExecuteLogout(object parameter)
        {
            bool sessionClosedLocally = false;
            try
            {
                string currentUsername = SessionManager.Instance.CurrentUsername;

                if (!string.IsNullOrEmpty(currentUsername))
                {
                    using (var authClient = new AuthenticationServiceClient())
                    {
                        try
                        {
                            authClient.LogOut(currentUsername);
                        }
                        catch (CommunicationException commEx)
                        {
                            Console.WriteLine($"Error de comunicación al cerrar sesión en servidor: {commEx.Message}");
                        }
                    }
                }

                SessionManager.Instance.CloseSession();
                sessionClosedLocally = true;
                MatchmakingClientManager.Instance.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado durante el cierre de sesión: {ex.Message}");
                if (!sessionClosedLocally)
                {
                    SessionManager.Instance.CloseSession();
                }
            }

            var mainView = new Main();
            mainView.WindowState = WindowState.Maximized;
            mainView.WindowStyle = WindowStyle.None;
            mainView.ResizeMode = ResizeMode.NoResize;
            mainView.Show();

            Window currentLobbyWindow = Application.Current.Windows.OfType<LobbyView>().FirstOrDefault();

            if (currentLobbyWindow != null)
            {
                currentLobbyWindow.Close();
            }
        }
    }
}