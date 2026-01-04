using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.HomePages;

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
                throw new ArgumentNullException(nameof(userEmail), Lang.alertEmailEmpty);
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
            var client = new AuthenticationServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.VerifyAccountAsync(_userEmail, VerificationCode);

                if (result.Success)
                {
                    ShowAlert(Lang.alertVerificationSuccess, Lang.alertActivationCompleteTitle, MessageBoxImage.Information);

                    OpenLoginWindow(parameter);

                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    ShowServiceError(result.ErrorCode);
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

        private static void OpenLoginWindow(object parameter)
        {
            var loginView = new LoginView();
            loginView.Show();

            if (parameter is Window verifyWindow)
            {
                verifyWindow.Close();
            }
        }

        private static void CloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}