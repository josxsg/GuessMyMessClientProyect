using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Model;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Lobby;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class SignUpViewModel : ViewModelBase
    {
        private string _username;
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

        private string _firstName;
        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                if (_firstName != value)
                {
                    _firstName = value; 
                    OnPropertyChanged();
                }
            }
        }

        private string _lastName;
        public string LastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                if (_lastName != value)
                {
                    _lastName = value; 
                    OnPropertyChanged();
                }
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value; 
                    OnPropertyChanged();
                }
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (_password != value)
                {
                    _password = value; 
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMale = true;
        public bool IsMale
        {
            get
            {
                return _isMale;
            }
            set
            {
                if (value && _isMale != value)
                {
                    _isMale = value; 
                    OnPropertyChanged(); 
                    ResetGender(1);
                }
            }
        }

        private bool _isFemale;
        public bool IsFemale
        {
            get
            {
                return _isFemale;
            }
            set
            {
                if (value && _isFemale != value)
                {
                    _isFemale = value; 
                    OnPropertyChanged(); 
                    ResetGender(2);
                }
            }
        }

        private bool _isNonBinary;
        public bool IsNonBinary
        {
            get
            {
                return _isNonBinary;
            }
            set
            {
                if (value && _isNonBinary != value)
                {
                    _isNonBinary = value; 
                    OnPropertyChanged(); 
                    ResetGender(3);
                }
            }
        }

        private int _selectedAvatarId = 1;
        public int SelectedAvatarId
        {
            get
            {
                return _selectedAvatarId;
            }
            set
            {
                if (_selectedAvatarId != value)
                {
                    _selectedAvatarId = value; 
                    OnPropertyChanged();
                }
            }
        }

        private BitmapImage _selectedAvatarImage;
        public BitmapImage SelectedAvatarImage
        {
            get
            {
                return _selectedAvatarImage;
            }
            set
            {
                if (_selectedAvatarImage != value)
                {
                    _selectedAvatarImage = value; OnPropertyChanged();
                }
            }
        }

        public ICommand SignUpCommand { get; }
        public ICommand SelectAvatarCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand ReturnCommand { get; }

        public SignUpViewModel()
        {
            SignUpCommand = new RelayCommand(ExecuteSignUp, CanExecuteSignUp);
            SelectAvatarCommand = new RelayCommand(OpenSelectAvatarDialog);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            ReturnCommand = new RelayCommand(ExecuteReturn);

            Task.Run(() => LoadDefaultAvatar());
        }

        private void ResetGender(int selectedGenderId)
        {
            if (_isMale != (selectedGenderId == 1))
            {
                _isMale = (selectedGenderId == 1); 
                OnPropertyChanged(nameof(IsMale));
            }

            if (_isFemale != (selectedGenderId == 2))
            {
                _isFemale = (selectedGenderId == 2); 
                OnPropertyChanged(nameof(IsFemale));
            }

            if (_isNonBinary != (selectedGenderId == 3))
            {
                _isNonBinary = (selectedGenderId == 3); 
                OnPropertyChanged(nameof(IsNonBinary));
            }
        }

        private async Task LoadDefaultAvatar()
        {
            var client = new UserProfileServiceClient();
            try
            {
                var avatars = await client.GetAvailableAvatarsAsync();
                if (avatars != null && avatars.Any())
                {
                    var defaultAvatar = avatars.FirstOrDefault(a => a.IdAvatar == 1) ?? avatars[0];
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SelectedAvatarId = defaultAvatar.IdAvatar;
                        SelectedAvatarImage = ConvertByteToImage(defaultAvatar.AvatarData);
                    });
                }
                client.Close();
            }
            catch
            {
                client.Abort();
            }
        }

        private async void ExecuteSignUp(object parameter)
        {
            if (!CanExecuteSignUp(parameter))
            {
                MessageBox.Show(
                    Lang.alertRequiredFields,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(Email))
            {
                MessageBox.Show(
                    Lang.alertInvalidEmailFormat,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsPasswordSecure(Password, out string passwordErrorKey))
            {
                string passwordErrorMessage = Lang.ResourceManager.GetString(passwordErrorKey) ?? Lang.alertPasswordGenericError;
                MessageBox.Show(
                    passwordErrorMessage,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int genderId = IsMale ? 1 : (IsFemale ? 2 : 3);

            var newUserProfile = new AuthService.UserProfileDto
            {
                Username = Username,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                GenderId = genderId,
                AvatarId = SelectedAvatarId
            };

            var client = new AuthenticationServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RegisterAsync(newUserProfile, Password);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"{Lang.alertRegistrationSuccess}\n{result.Message}",
                        Lang.alertSuccessTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    OpenVerificationDialog(parameter);
                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    MessageBox.Show
                        (result.Message,
                        Lang.alertRegistrationErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (FaultException<GuessMyMessClient.AuthService.ServiceFaultDto> fex)
            {
                string titulo = Lang.alertRegistrationErrorTitle;

                if (fex.Detail.ErrorType == GuessMyMessClient.AuthService.ServiceErrorType.DuplicateRecord ||
                    fex.Detail.ErrorType == GuessMyMessClient.AuthService.ServiceErrorType.UserAlreadyExists ||
                    fex.Detail.ErrorType == GuessMyMessClient.AuthService.ServiceErrorType.EmailAlreadyRegistered)
                {
                    titulo = Lang.alertInputErrorTitle;
                }

                MessageBox.Show(
                    fex.Detail.Message,
                    titulo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (FaultException)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private bool CanExecuteSignUp(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   (IsMale || IsFemale || IsNonBinary);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s\.]{2,}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                return regex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException) 
            { 
                return false;
            }
        }

        private static bool IsPasswordSecure(string password, out string errorLangKey)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errorLangKey = "alertPasswordEmpty"; 
                return false;
            }

            if (password.Length < 8)
            {
                errorLangKey = "alertPasswordTooShort"; 
                return false;
            }

            if (password.Length > 25)
            {
                errorLangKey = "alertPasswordTooLong"; 
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                errorLangKey = "alertPasswordNeedsUpper"; 
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                errorLangKey = "alertPasswordNeedsLower"; 
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                errorLangKey = "alertPasswordNeedsDigit"; 
                return false;
            }

            if (password.All(char.IsLetterOrDigit))
            {
                errorLangKey = "alertPasswordNeedsSpecial"; 
                return false;
            }
            if (!password.Contains(","))
            {
                errorLangKey = "alertPasswordNeedsComma";
                return false;

            }

            errorLangKey = null;
            return true;
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
        
        private void OpenSelectAvatarDialog(object parameter)
        {
            var selectAvatarView = new SelectAvatarView();
            var selectAvatarViewModel = new SelectAvatarViewModel(SelectedAvatarId);
            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;
            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();
            selectAvatarViewModel.AvatarSelected -= OnAvatarSelected;
        }

        private void OnAvatarSelected(AvatarModel avatar)
        {
            if (avatar != null)
            {
                SelectedAvatarId = avatar.Id;
                SelectedAvatarImage = avatar.ImageSource;
            }
        }

        private void OpenVerificationDialog(object parameter)
        {
            var verifyView = new VerifyByCodeView();
            verifyView.DataContext = new VerifyByCodeViewModel(Email);
            verifyView.Show();

            if (parameter is Window signUpWindow)
            {
                signUpWindow.Close();
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

        private static void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.Show();
                currentWindow.Close();
            }
        }
    }
}
