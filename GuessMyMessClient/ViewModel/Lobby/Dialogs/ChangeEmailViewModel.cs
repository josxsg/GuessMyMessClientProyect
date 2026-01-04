using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Support;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    internal class ChangeEmailViewModel : ViewModelBase
    {
        private readonly string _username;
        private readonly Action<string> _emailUpdateCallback;
        private string _newEmail;

        public string NewEmail
        {
            get
            {
                return _newEmail;
            }
            set
            {
                if (_newEmail != value)
                {
                    _newEmail = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CloseCommand { get; }

        public ChangeEmailViewModel(string username, Action<string> emailUpdateCallback)
        {
            _username = username;
            _emailUpdateCallback = emailUpdateCallback;
            ConfirmCommand = new RelayCommand(ExecuteConfirm, CanExecuteConfirm);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private bool CanExecuteConfirm(object obj)
        {
            return !string.IsNullOrWhiteSpace(NewEmail) && NewEmail.Contains("@");
        }

        private async void ExecuteConfirm(object parameter)
        {
            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            if (!InputValidator.IsValidEmail(NewEmail))
            {
                ShowAlert(Lang.alertInvalidEmailFormat, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await client.RequestChangeEmailAsync(_username, NewEmail);

                if (result.Success)
                {
                    ShowAlert(result.Message, Lang.alertCodeSentTitle, MessageBoxImage.Information);

                    var verifyVM = new VerifyChangesByCodeViewModel(
                        VerifyChangesByCodeViewModel.VerificationMode.Email,
                        _username,
                        NewEmail,
                        _emailUpdateCallback
                    );

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