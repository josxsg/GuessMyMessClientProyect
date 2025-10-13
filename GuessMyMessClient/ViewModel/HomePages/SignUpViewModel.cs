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
        // Propiedades de la vista
        private string _username;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        private string _firstName;
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); } }
        private string _lastName;
        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); } }
        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        private string _password; // Enlazada a través del PasswordBoxHelper
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        // Propiedades de la UI para Género
        private bool _isMale;
        public bool IsMale { get => _isMale; set { _isMale = value; OnPropertyChanged(); } }
        private bool _isFemale;
        public bool IsFemale { get => _isFemale; set { _isFemale = value; OnPropertyChanged(); } }
        private bool _isNonBinary;
        public bool IsNonBinary { get => _isNonBinary; set { _isNonBinary = value; OnPropertyChanged(); } }


        // Propiedades de Avatar Seleccionado
        private int _selectedAvatarId = 1; // ID 1 = Avatar por defecto
        public int SelectedAvatarId { get => _selectedAvatarId; set { _selectedAvatarId = value; OnPropertyChanged(); } }
        private BitmapImage _selectedAvatarImage;
        public BitmapImage SelectedAvatarImage { get => _selectedAvatarImage; set { _selectedAvatarImage = value; OnPropertyChanged(); } }

        // Comandos
        public ICommand SignUpCommand { get; }
        public ICommand SelectAvatarCommand { get; }

        public SignUpViewModel()
        {
            SignUpCommand = new RelayCommand(ExecuteSignUp, CanExecuteSignUp);
            SelectAvatarCommand = new RelayCommand(OpenSelectAvatarDialog);
            // Iniciar con selección de género por defecto (o ninguno)
            IsMale = true;
        }

        private bool CanExecuteSignUp(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password); // La contraseña se actualiza mediante el helper
        }

        private async void ExecuteSignUp(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Error de Validación");
                return;
            }

            // Mapeo de género (asumiendo 1=Male, 2=Female, 3=NonBinary)
            int genderId = 0;
            if (IsMale) genderId = 1;
            else if (IsFemale) genderId = 2;
            else if (IsNonBinary) genderId = 3;


            UserProfileDto newProfile = new UserProfileDto
            {
                username = Username,
                firstName = FirstName,
                lastName = LastName,
                email = Email,
                genderId = genderId,
                avatarId = SelectedAvatarId
            };

            // Iniciar el cliente WCF en un bloque using
            using (AuthenticationServiceClient client = new AuthenticationServiceClient())
            {
                try
                {
                    // Llamar al servicio de registro (Register)
                    OperationResultDto result = await client.RegisterAsync(newProfile, Password);

                    if (result.success)
                    {
                        MessageBox.Show("Registro exitoso. " + result.message, "Éxito");
                        // Abrir la ventana de verificación al cerrar la ventana actual
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

        private void OpenSelectAvatarDialog(object parameter)
        {
            var selectAvatarView = new SelectAvatarView();
            var selectAvatarViewModel = new SelectAvatarViewModel();

            // Suscribirse al evento para recibir el avatar seleccionado
            selectAvatarViewModel.AvatarSelected += OnAvatarSelected;

            selectAvatarView.DataContext = selectAvatarViewModel;
            selectAvatarView.ShowDialog();

            // Limpiar la suscripción
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
            /// El orden correcto es ABRIR la nueva ventana, y luego CERRAR la vieja.

            // 1. Abrir la ventana de verificación (ANTES de cerrar la actual)
            var verifyView = new VerifyByCodeView();
            verifyView.DataContext = new VerifyByCodeViewModel(Email);
            verifyView.Show();

            // 2. Cerrar la ventana de registro
            if (parameter is Window signUpWindow)
            {
                signUpWindow.Close();
            }
        }
    }
}
