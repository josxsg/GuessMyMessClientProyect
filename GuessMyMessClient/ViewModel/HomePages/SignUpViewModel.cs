using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.Model;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using GuessMyMessClient.View.HomePages;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Lobby;

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
        private bool _isMale;
        public bool IsMale { get => _isMale; set { _isMale = value; OnPropertyChanged(); } }
        private bool _isFemale;
        public bool IsFemale { get => _isFemale; set { _isFemale = value; OnPropertyChanged(); } }
        private bool _isNonBinary;
        public bool IsNonBinary { get => _isNonBinary; set { _isNonBinary = value; OnPropertyChanged(); } }


        private int _selectedAvatarId = 1; 
        public int SelectedAvatarId { get => _selectedAvatarId; set { _selectedAvatarId = value; OnPropertyChanged(); } }

        private BitmapImage _selectedAvatarImage;
        public BitmapImage SelectedAvatarImage
        {
            get => _selectedAvatarImage;
            set { _selectedAvatarImage = value; OnPropertyChanged(); }
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
            IsMale = true; 

            LoadDefaultAvatar();
        }

        private async void LoadDefaultAvatar()
        {
            try
            {
                using (var client = new ProfileService.UserProfileServiceClient())
                {
                    var avatars = await client.GetAvailableAvatarsAsync();
                    if (avatars.Any())
                    {
                        var defaultAvatar = avatars.First();
                        SelectedAvatarId = defaultAvatar.idAvatar;
                        SelectedAvatarImage = SelectAvatarViewModel.ConvertByteToImage(defaultAvatar.avatarData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo cargar el avatar por defecto: " + ex.Message);
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
                   !string.IsNullOrWhiteSpace(Password); 
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


            AuthService.UserProfileDto newProfile = new AuthService.UserProfileDto
            {
                Username = Username,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                GenderId = genderId,
                AvatarId = SelectedAvatarId
            };

            using (AuthenticationServiceClient client = new AuthenticationServiceClient())
            {
                try
                {
                    OperationResultDto result = await client.RegisterAsync(newProfile, Password);

                    if (result.success)
                    {
                        MessageBox.Show("Registro exitoso. " + result.message, "Éxito");
                        
                        OpenVerificationDialog(parameter);
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error de Registro");
                    }
                }
                catch (FaultException<string> ex)
                {
                    MessageBox.Show(ex.Message, "Error WCF (Lógica)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error de conexión: {ex.Message}", "Error WCF");
                }
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
        private void ExecuteReturn(object parameter)
        {
            if (parameter is Window currentWindow)
            {
                var welcomeView = new WelcomeView();
                welcomeView.WindowState = currentWindow.WindowState;

                welcomeView.Show();
                currentWindow.Close();
            }
        }
    }
}
