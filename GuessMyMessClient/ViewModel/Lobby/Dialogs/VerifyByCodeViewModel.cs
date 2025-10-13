using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.AuthService;
using GuessMyMessClient.View.HomePages;
using System.Windows.Input;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    public class VerifyByCodeViewModel : ViewModelBase
    {
        private readonly string _userEmail;

        private string _verificationCode;
        public string VerificationCode
        {
            get => _verificationCode;
            set { _verificationCode = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }
        public ICommand CloseCommand { get; }

        public VerifyByCodeViewModel(string userEmail)
        {
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

                    if (result.success)
                    {
                        MessageBox.Show("Cuenta verificada. Por favor, inicia sesión.", "Activación Completa");
                        // Cerrar la ventana de verificación y abrir la ventana de Login
                        OpenLoginWindow(parameter);
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error de Verificación");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error de conexión: {ex.Message}", "Error WCF");
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
