using System;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    internal class VerifyChangesByCodeViewModel : ViewModelBase
    {
        public enum VerificationMode { Email, Password }
        private readonly VerificationMode _mode;
        private readonly string _username;
        private readonly string _payload;
        private readonly Action<string> _emailUpdateCallback;
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

        public VerifyChangesByCodeViewModel(VerificationMode mode, string username, string payload, Action<string> emailUpdateCallback)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (mode == VerificationMode.Email && !IsValidEmail(payload))
            {
                throw new ArgumentException(Lang.alertNewEmailIvalideFormat, nameof(payload));
            }

            _mode = mode;
            _username = username;
            _payload = payload;
            _emailUpdateCallback = emailUpdateCallback;

            VerifyCommand = new RelayCommand(ExecuteVerify, CanExecuteVerify);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private bool CanExecuteVerify(object obj)
        {
            return !string.IsNullOrWhiteSpace(VerificationCode) &&
                   VerificationCode.Length == 6 &&
                   VerificationCode.All(char.IsDigit);
        }

        private async void ExecuteVerify(object parameter)
        {
            if (!CanExecuteVerify(null))
            {
                ShowAlert(Lang.alertInvalidCodeFormat, Lang.alertInvalidCodeTitle, MessageBoxImage.Warning);
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                OperationResultDto result;

                if (_mode == VerificationMode.Email)
                {
                    result = await client.ConfirmChangeEmailAsync(_username, VerificationCode);
                    if (result.Success)
                    {
                        _emailUpdateCallback?.Invoke(_payload);
                    }
                }
                else
                {
                    result = await client.ConfirmChangePasswordAsync(_username, _payload, VerificationCode);
                }

                if (result.Success)
                {
                    ShowAlert(result.Message, Lang.alertSuccessTitle, MessageBoxImage.Information);

                    ExecuteClose(parameter);
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

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s\.]{2,}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                return regex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}