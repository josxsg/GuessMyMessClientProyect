using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using ServiceProfileFault = GuessMyMessClient.ProfileService.ServiceFaultDto;

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
        public ICommand AddSocialNetworkCommand { get; }

        public ProfileViewModel(UserProfileDto initialProfileData)
        {
            _profileData = initialProfileData ?? throw new ArgumentNullException(nameof(initialProfileData));
            ChangeEmailCommand = new RelayCommand(ExecuteChangeEmail);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            AddSocialNetworkCommand = new RelayCommand(ExecuteAddSocialNetwork);
        }

        private void ExecuteAddSocialNetwork(object parameter)
        {
            if (parameter is string networkName)
            {
                // 1. Instanciamos el ViewModel del Dialog
                // Le pasamos el nombre (ej: "Discord") y una función lambda que se ejecutará si le dan "Guardar"
                var dialogVM = new AddSocialNetworkViewModel(networkName, (linkIngresado) =>
                {
                    // AQUÍ RECIBES EL LINK CUANDO EL USUARIO LE DA GUARDAR
                    // TODO: En el siguiente paso implementaremos la llamada al servidor para guardar esto en la BD.
                    MessageBox.Show($"Guardando en BD...\nRed: {networkName}\nLink: {linkIngresado}");
                });

                // 2. Instanciamos la Vista del Dialog
                var dialogView = new AddSocialNetworkView
                {
                    DataContext = dialogVM,
                    Owner = Application.Current.MainWindow // Para que sea modal sobre la ventana principal
                };

                // 3. Mostramos la ventana como Modal (bloquea la de atrás hasta que se cierra)
                dialogView.ShowDialog();
            }
        }
        private async void ExecuteSaveProfile(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                MessageBox.Show(
                    Lang.alertProfileMandatoryFields,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

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

                    client.Close();
                    isSuccess = true;
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
            catch (FaultException<ServiceProfileFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertProfileUpdateErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (FaultException)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertProfileUpdateErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertProfileUpdateErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show(
                    Lang.alertProfileUpdateUnknownError,
                    Lang.alertProfileUpdateErrorTitle,
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

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RequestChangePasswordAsync(_profileData.Username);

                if (result.Success)
                {
                    var changePasswordVM = new ChangePasswordViewModel(_profileData.Username);
                    var changePasswordView = new ChangePasswordView
                    {
                        DataContext = changePasswordVM
                    };

                    client.Close();
                    isSuccess = true;

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
            catch (FaultException<ServiceProfileFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (FaultException)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertPasswordRequestErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show(
                    Lang.alertPasswordRequestUnknownError,
                    Lang.alertPasswordRequestErrorTitle,
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
    }
}