using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Support;

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
                ShowAlert(Lang.alertPasswordControlsNotFound, Lang.alertErrorTitle, MessageBoxImage.Error);
                return;
            }

            string newPassword = newPasswordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (!InputValidator.IsPasswordSecure(newPassword, out string passwordErrorKey))
            {
                string passwordErrorMessage = Lang.ResourceManager.GetString(passwordErrorKey) ?? Lang.alertPasswordGenericError;
                ShowAlert(passwordErrorMessage, Lang.alertPasswordNotSecureTitle, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowAlert(Lang.alertPasswordsDoNotMatch, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RequestChangePasswordAsync(_username);

                if (result.Success)
                {
                    ShowAlert(result.Message, Lang.alertCodeSentTitle, MessageBoxImage.Information);

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
                    ShowServiceError((AuthService.ServiceErrorType)result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
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