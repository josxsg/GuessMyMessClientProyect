using GuessMyMessClient.ProfileService; 
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
            get => _verificationCode;
            set { _verificationCode = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }
        public ICommand CloseCommand { get; }

        
        public VerifyChangesByCodeViewModel(VerificationMode mode, string username, string payload, Action<string> emailUpdateCallback)
        {
            _mode = mode;
            _username = username;
            _payload = payload; 
            _emailUpdateCallback = emailUpdateCallback;

            VerifyCommand = new RelayCommand(ExecuteVerify, CanExecuteVerify);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private bool CanExecuteVerify(object obj)
        {
            return !string.IsNullOrWhiteSpace(VerificationCode) && VerificationCode.Length == 6 && VerificationCode.All(char.IsDigit);
        }

        private async void ExecuteVerify(object parameter)
        {
            if (!CanExecuteVerify(null))
            {
                MessageBox.Show("El código de verificación debe ser de 6 dígitos numéricos.", "Código Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        if (result.success)
                        {
                            _emailUpdateCallback?.Invoke(_payload); 
                        }
                    }
                    else 
                    {
                        result = await client.ConfirmChangePasswordAsync(_username, _payload, VerificationCode);
                    }

                    if (result.success)
                    {
                        MessageBox.Show(result.message, "Éxito");
                        ExecuteClose(parameter); 
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error de Verificación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de comunicación: {ex.Message}", "Error WCF");
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