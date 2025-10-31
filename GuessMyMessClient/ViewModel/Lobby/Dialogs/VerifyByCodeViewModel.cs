using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.View.HomePages;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using System.ServiceModel;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    public class VerifyByCodeViewModel : ViewModelBase
    {
        private readonly string _userEmail;
        private string _verificationCode;
        public string VerificationCode
        {
            get
            {
                return _verificationCode;
            }
            set
            {
                if (_verificationCode != value)
                {
                    _verificationCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand VerifyCommand { get; }
        public ICommand CloseCommand { get; }

        public VerifyByCodeViewModel(string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new ArgumentNullException(nameof(userEmail), "User email cannot be empty for verification.");
            }
            _userEmail = userEmail;

            VerifyCommand = new RelayCommand(ExecuteVerify, CanExecuteVerify);
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private bool CanExecuteVerify(object parameter)
        {
            return !string.IsNullOrWhiteSpace(VerificationCode);
        }

        private async void ExecuteVerify(object parameter)
        {
            using (AuthenticationServiceClient client = new AuthenticationServiceClient())
            {
                try
                {
                    OperationResultDto result = await client.VerifyAccountAsync(_userEmail, VerificationCode);

                    if (result.Success)
                    {
                        MessageBox.Show(
                            Lang.alertVerificationSuccess,
                            Lang.alertActivationCompleteTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        OpenLoginWindow(parameter);
                    }
                    else
                    {
                        MessageBox.Show(
                            result.Message,
                            Lang.alertVerificationErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (FaultException fexGeneral)
                {
                    MessageBox.Show(
                        Lang.alertServerErrorMessage,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"WCF Error during verification: {fexGeneral.Message}");
                }
                catch (EndpointNotFoundException ex)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertConnectionErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Connection Error during verification: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertUnknownErrorMessage,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Unknown Error during verification: {ex.Message}");
                }
            }
        }

        private void OpenLoginWindow(object parameter)
        {
            var loginView = new LoginView();
            loginView.Show();

            if (parameter is Window verifyWindow)
            {
                verifyWindow.Close();
            }
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}
