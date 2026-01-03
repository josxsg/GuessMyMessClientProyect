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
using GuessMyMessClient.ViewModel.Support;

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
                    await Application.Current.Dispatcher.InvokeAsync(() =>
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
            if (!IsInputValid(parameter))
            {
                return;
            }

            TrimInputFields();
            var newUserProfile = CreateUserProfileDto();
            var client = new AuthenticationServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RegisterAsync(newUserProfile, Password);
                isSuccess = HandleRegistrationResult(result, client, parameter);
            }
            catch (Exception ex)
            {
                HandleRegistrationException(ex);
            }
            finally
            {
                FinalizeClientState(client, isSuccess);
            }
        }

        private bool IsInputValid(object parameter)
        {
            if (!CanExecuteSignUp(parameter))
            {
                ShowWarning(Lang.alertRequiredFields);
                return false;
            }

            if (!InputValidator.IsValidEmail(Email))
            {
                ShowWarning(Lang.alertInvalidEmailFormat);
                return false;
            }

            if (!InputValidator.IsValidName(FirstName) || !InputValidator.IsValidName(LastName))
            {
                ShowWarning(FirstName == null || !InputValidator.IsValidName(FirstName) ? Lang.alertNameInvalid : Lang.alertLastNameInvalid);
                return false;
            }

            if (!InputValidator.IsValidUsername(Username, out string userErr))
            {
                ShowWarning(Lang.ResourceManager.GetString(userErr) ?? Lang.alertUsernameGenericError);
                return false;
            }

            if (!InputValidator.IsPasswordSecure(Password, out string passErr))
            {
                ShowWarning(Lang.ResourceManager.GetString(passErr) ?? Lang.alertPasswordGenericError);
                return false;
            }

            return true;
        }

        private void TrimInputFields()
        {
            FirstName = FirstName?.Trim();
            LastName = LastName?.Trim();
            Username = Username?.Trim();
            Email = Email?.Trim();
        }

        private AuthService.UserProfileDto CreateUserProfileDto()
        {
            int genderId;
            if (IsMale)
            {
                genderId = 1;
            }
            else if (IsFemale)
            {
                genderId = 2;
            }
            else
            {
                genderId = 3; // Otro / Prefiere no decir
            }
            return new AuthService.UserProfileDto
            {
                Username = Username,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                GenderId = genderId,
                AvatarId = SelectedAvatarId
            };
        }

        private bool HandleRegistrationResult(AuthService.OperationResultDto result, AuthenticationServiceClient client, object parameter)
        {
            if (result.Success)
            {
                MessageBox.Show($"{Lang.alertRegistrationSuccess}\n{result.Message}", Lang.alertSuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                OpenVerificationDialog(parameter);
                client.Close();
                return true;
            }

            MessageBox.Show(result.Message, Lang.alertRegistrationErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        private void HandleRegistrationException(Exception ex)
        {
            if (ex is FaultException<GuessMyMessClient.AuthService.ServiceFaultDto> fex)
            {
                bool isInputError = fex.Detail.ErrorType == AuthService.ServiceErrorType.DuplicateRecord ||
                                   fex.Detail.ErrorType == AuthService.ServiceErrorType.UserAlreadyExists ||
                                   fex.Detail.ErrorType == AuthService.ServiceErrorType.EmailAlreadyRegistered;

                MessageBox.Show(fex.Detail.Message, isInputError ? Lang.alertInputErrorTitle : Lang.alertRegistrationErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (ex is FaultException)
            {
                MessageBox.Show(Lang.alertServerErrorMessage, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(Lang.alertConnectionErrorMessage, Lang.alertConnectionErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(Lang.alertUnknownErrorMessage, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FinalizeClientState(AuthenticationServiceClient client, bool isSuccess)
        {
            if (!isSuccess && client.State != CommunicationState.Closed)
            {
                client.Abort();
            }
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(message, Lang.alertInputErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
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
