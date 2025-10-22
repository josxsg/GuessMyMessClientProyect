using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs; 
using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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


        private void ExecuteConfirm(object parameter)
        {
            if (!(parameter is Window window)) return;

            var newPasswordBox = window.FindName("NewPasswordBox") as PasswordBox;
            var confirmPasswordBox = window.FindName("ConfirmPasswordBox") as PasswordBox;

            if (newPasswordBox == null || confirmPasswordBox == null)
            {
                MessageBox.Show("Los campos no pueden estar vacios.", "Campos Vacios");
                return;
            }

            string newPassword = newPasswordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Contraseña Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            var verifyVM = new VerifyChangesByCodeViewModel( 
                VerifyChangesByCodeViewModel.VerificationMode.Password,
                _username,
                newPassword,
                null
            );

            var verifyView = new VerifyChangesByCodeView { DataContext = verifyVM };

            ExecuteClose(parameter);
            verifyView.ShowDialog();
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