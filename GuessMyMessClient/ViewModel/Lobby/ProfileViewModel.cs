using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using System.ServiceModel;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly UserProfileDto _profileData;

        public string FirstName
        {
            get
            {
                return _profileData.FirstName;
            }
            set
            {
                if (_profileData.FirstName != value)
                {
                    _profileData.FirstName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string LastName
        {
            get
            {
                return _profileData.LastName;
            }
            set
            {
                if (_profileData.LastName != value)
                {
                    _profileData.LastName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username => _profileData?.Username;

        public string Email
        {
            get
            {
                return _profileData?.Email;
            }
            set
            {
                if (_profileData != null && _profileData.Email != value)
                {
                    _profileData.Email = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMale => _profileData?.GenderId == 1;
        public bool IsFemale => _profileData?.GenderId == 2;
        public bool IsNonBinary => _profileData?.GenderId == 3;

        public ICommand ChangeEmailCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public ProfileViewModel(UserProfileDto initialProfileData)
        {
            _profileData = initialProfileData ?? throw new ArgumentNullException(nameof(initialProfileData), "Initial profile data cannot be null.");

            ChangeEmailCommand = new RelayCommand(ExecuteChangeEmail);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
        }

        private async void ExecuteSaveProfile(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                MessageBox.Show(
                    Lang.alertProfileMandatoryFields,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var client = new UserProfileServiceClient())
            {
                try
                {
                    OperationResultDto result = await client.UpdateProfileAsync(_profileData.Username, _profileData);

                    if (result.Success)
                    {
                        MessageBox.Show(
                            Lang.alertProfileUpdateSuccess,
                            Lang.alertSuccessTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            result.Message,
                            Lang.alertProfileUpdateErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (FaultException fexGeneral)
                {
                    MessageBox.Show(
                        Lang.alertServerErrorMessage,
                        Lang.alertProfileUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"WCF Error saving profile: {fexGeneral.Message}");
                }
                catch (EndpointNotFoundException ex)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertProfileUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Connection Error saving profile: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertProfileUpdateUnknownError,
                        Lang.alertProfileUpdateErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Error de comunicación al guardar perfil: {ex.Message}");
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
            MessageBox.Show(
                Lang.alertPasswordChangeCodeInfo,
                Lang.alertPasswordChangeTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    var result = await client.RequestChangePasswordAsync(_profileData.Username);

                    if (result.Success)
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
                        MessageBox.Show(
                            result.Message,
                            Lang.alertPasswordRequestErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error requesting password change: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error requesting password change: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertPasswordRequestUnknownError,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Error de comunicación al solicitar cambio contraseña: {ex.Message}");
            }
        }
    }
}
