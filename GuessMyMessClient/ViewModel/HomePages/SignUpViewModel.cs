using System;
using System.Linq; 
using System.ServiceModel;
using System.Threading.Tasks; 
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GuessMyMessClient.AuthService; 
using GuessMyMessClient.ProfileService; 
using GuessMyMessClient.Model; 
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Lobby;
using GuessMyMessClient.ViewModel.Lobby.Dialogs; 

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class SignUpViewModel : ViewModelBase
    {
        private string _username;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        private string _firstName;
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); } }
        private string _lastName;
        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); } }
        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        private string _password;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private bool _isMale = true; 
        public bool IsMale { get => _isMale; set { _isMale = value; if (value) ResetGender(1); OnPropertyChanged(); } }
        private bool _isFemale;
        public bool IsFemale { get => _isFemale; set { _isFemale = value; if (value) ResetGender(2); OnPropertyChanged(); } }
        private bool _isNonBinary;
        public bool IsNonBinary { get => _isNonBinary; set { _isNonBinary = value; if (value) ResetGender(3); OnPropertyChanged(); } }

        private int _selectedAvatarId = 1;
        public int SelectedAvatarId { get => _selectedAvatarId; set { _selectedAvatarId = value; OnPropertyChanged(); } }
        private BitmapImage _selectedAvatarImage;
        public BitmapImage SelectedAvatarImage { get => _selectedAvatarImage; set { _selectedAvatarImage = value; OnPropertyChanged(); } }

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

            LoadDefaultAvatar();
        }

        private void ResetGender(int selectedGenderId)
        {
            IsMale = (selectedGenderId == 1);
            IsFemale = (selectedGenderId == 2);
            IsNonBinary = (selectedGenderId == 3);
            OnPropertyChanged(nameof(IsMale));
            OnPropertyChanged(nameof(IsFemale));
            OnPropertyChanged(nameof(IsNonBinary));
        }

        private async void LoadDefaultAvatar()
        {
            var client = new UserProfileServiceClient(); 
            try
            {
                var avatars = await client.GetAvailableAvatarsAsync();
                if (avatars != null && avatars.Any())
                {
                    var defaultAvatar = avatars.FirstOrDefault(a => a.idAvatar == 1) ?? avatars.First();
                    SelectedAvatarId = defaultAvatar.idAvatar;
                    SelectedAvatarImage = ConvertByteToImage(defaultAvatar.avatarData);
                }
                client.Close();
            }
            catch (FaultException ex)
            {
                MessageBox.Show($"Error WCF al cargar avatar: {ex.Message}", "Error Servidor");
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar el avatar por defecto: {ex.Message}", "Error Conexión");
                client.Abort();
            }
        }

        private void OpenSelectAvatarDialog(object parameter)
        {
            var selectAvatarView = new SelectAvatarView();
            var selectAvatarViewModel = new SelectAvatarViewModel(this.SelectedAvatarId);
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

        private bool CanExecuteSignUp(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password.Length >= 6 && 
                   (IsMale || IsFemale || IsNonBinary); 
        }

        private async void ExecuteSignUp(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Error de Validación");
                return;
            }

            int genderId = 0;
            if (IsMale) genderId = 1;
            else if (IsFemale) genderId = 2;
            else if (IsNonBinary) genderId = 3; 

            AuthService.UserProfileDto newUserProfile = new AuthService.UserProfileDto
            {
                Username = Username,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                GenderId = genderId,
                AvatarId = SelectedAvatarId
            };

            var client = new AuthenticationServiceClient();
            try
            {
                AuthService.OperationResultDto result = await client.RegisterAsync(newUserProfile, Password);

                if (result.success)
                {
                    MessageBox.Show("Registro exitoso. " + result.message, "Éxito");
                    OpenVerificationDialog(parameter);
                }
                else
                {
                    MessageBox.Show(result.message, "Error de Registro");
                }
                client.Close(); 
            }
            catch (FaultException<string> fex) 
            {
                MessageBox.Show($"Error del servidor: {fex.Detail}", "Error WCF (Lógica)");
                client.Abort();
            }
            catch (FaultException fexGeneral) 
            {
                MessageBox.Show($"Error WCF: {fexGeneral.Message}", "Error WCF");
                client.Abort();
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Error de conexión o inesperado: {ex.Message}", "Error Crítico");
                client.Abort(); 
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

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window) 
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

        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.Show();
                currentWindow.Close();
            }
        }

        public static BitmapImage ConvertByteToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new System.IO.MemoryStream(imageBytes)) 
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
    }
}