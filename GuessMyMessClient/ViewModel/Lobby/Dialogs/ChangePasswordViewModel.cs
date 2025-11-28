using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.ViewModel.Support;
using ServiceProfileFault = GuessMyMessClient.ProfileService.ServiceFaultDto;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    internal class ChangePasswordViewModel : ViewModelBase
    {
        private readonly string _username;

        public ICommand ConfirmCommand { get; }
        public ICommand CloseCommand { get; }

        public ChangePasswordViewModel(string username)
        {
            _username = username;
            ConfirmCommand = new RelayCommand(ExecuteConfirm);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private async void ExecuteConfirm(object parameter)
        {
            if (!(parameter is Window window))
            {
                return;
            }

            var newPasswordBox = window.FindName("NewPasswordBox") as PasswordBox;
            var confirmPasswordBox = window.FindName("ConfirmPasswordBox") as PasswordBox;

            if (newPasswordBox == null || confirmPasswordBox == null)
            {
                MessageBox.Show(
                    Lang.alertPasswordControlsNotFound, 
                    Lang.alertErrorTitle, 
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string newPassword = newPasswordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (!InputValidator.IsPasswordSecure(newPassword, out string passwordErrorKey))
            {
                string passwordErrorMessage = Lang.ResourceManager.GetString(passwordErrorKey) ?? Lang.alertPasswordGenericError;
                MessageBox.Show(
                    passwordErrorMessage,
                    Lang.alertPasswordNotSecureTitle, 
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show(
                    Lang.alertPasswordsDoNotMatch,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RequestChangePasswordAsync(_username);

                if (result.Success)
                {
                    MessageBox.Show(
                        result.Message,
                        Lang.alertCodeSentTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    var verifyVM = new VerifyChangesByCodeViewModel(
                        VerifyChangesByCodeViewModel.VerificationMode.Password,
                        _username,
                        newPassword,
                        null);

                    var verifyView = new VerifyChangesByCodeView { DataContext = verifyVM };

                    ExecuteClose(parameter);
                    verifyView.ShowDialog();

                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    MessageBox.Show(
                        result.Message, 
                        Lang.alertErrorTitle, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                }
            }
            catch (FaultException<ServiceProfileFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message, 
                    Lang.alertErrorTitle, 
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
            catch (Exception)
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

        private static void ExecuteClose(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}
