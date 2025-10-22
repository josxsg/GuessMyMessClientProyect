using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.ProfileService;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class ProfileViewModel : ViewModelBase
    {
        private UserProfileDto _profileData;

        public string FirstName
        {
            get => _profileData.FirstName;
            set { _profileData.FirstName = value; OnPropertyChanged(); }
        }
        public string LastName
        {
            get => _profileData.LastName;
            set { _profileData.LastName = value; OnPropertyChanged(); }
        }

        public string Username => _profileData.Username;
        public string Email
        {
            get => _profileData.Email;
            set { _profileData.Email = value; OnPropertyChanged(); }
        }

        public bool IsMale => _profileData.GenderId == 1;
        public bool IsFemale => _profileData.GenderId == 2;
        public bool IsNonBinary => _profileData.GenderId == 3;

        public ICommand ChangeEmailCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SaveProfileCommand { get; } 

        
        public ProfileViewModel(UserProfileDto initialProfileData)
        {
            _profileData = initialProfileData ?? throw new ArgumentNullException(nameof(initialProfileData));

            ChangeEmailCommand = new RelayCommand(ExecuteChangeEmail);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
        }

        private async void ExecuteSaveProfile(object parameter)
        {
            using (var client = new UserProfileServiceClient())
            {
                try
                {
                    OperationResultDto result = await client.UpdateProfileAsync(_profileData.Username, _profileData);

                    if (result.success)
                    {
                        MessageBox.Show("Cambios guardados correctamente.", "Éxito");
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error al guardar perfil");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error de comunicación: {ex.Message}", "Error WCF");
                }
            }
        }

        private void ExecuteChangeEmail(object parameter)
        {
            var changeEmailVM = new ChangeEmailViewModel(_profileData.Username, (newEmail) =>
            {
                Email = newEmail;
            });

            var changeEmailView = new ChangeEmailView
            {
                DataContext = changeEmailVM
            };
            changeEmailView.ShowDialog();
        }

        private async void ExecuteChangePassword(object parameter)
        {
            MessageBox.Show("Se solicitará un código de verificación a tu correo registrado.", "Cambio de Contraseña", MessageBoxButton.OK, MessageBoxImage.Information);

            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    var result = await client.RequestChangePasswordAsync(_profileData.Username);

                    if (result.success)
                    {
                        var changePasswordVM = new ChangePasswordViewModel(_profileData.Username);
                        var changePasswordView = new ChangePasswordView
                        {
                            DataContext = changePasswordVM
                        };
                        changePasswordView.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de comunicación: {ex.Message}", "Error WCF");
            }
        }

    }
}
