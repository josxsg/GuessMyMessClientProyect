﻿using GuessMyMessClient.ProfileService;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using System.ServiceModel;

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

        private bool IsValidEmail(string email)
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
            if (mode == VerificationMode.Email && emailUpdateCallback == null)
            {
                Console.WriteLine("Advertencia: Callback de actualización de email no proporcionado para el modo Email.");
            }
            if (mode == VerificationMode.Email && !IsValidEmail(payload))
            {
                throw new ArgumentException("El nuevo email proporcionado (payload) tiene un formato inválido.", nameof(payload));
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
                MessageBox.Show(
                    Lang.alertInvalidCodeFormat,
                    Lang.alertInvalidCodeTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var client = new UserProfileServiceClient())
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
                        MessageBox.Show(
                            result.Message,
                            Lang.alertSuccessTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        ExecuteClose(parameter);
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
            }
            catch (FaultException fexGeneral)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"WCF Error during change confirmation: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error during change confirmation: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Unknown Error during change confirmation: {ex.Message}");
            }
        }

        private void ExecuteClose(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}
