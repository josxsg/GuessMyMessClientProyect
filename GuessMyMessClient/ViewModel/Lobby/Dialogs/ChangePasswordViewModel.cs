using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.Linq;
using System.Security;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;

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

            errorLangKey = null;
            return true;
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

            if (!IsPasswordSecure(newPassword, out string passwordErrorKey))
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

            try
            {
                using (var client = new UserProfileServiceClient())
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
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error requesting password change code: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error requesting password change code: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Unknown Error requesting password change code: {ex.Message}");
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
