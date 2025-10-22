using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs; 
using System;
using System.Windows;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    internal class ChangeEmailViewModel : ViewModelBase
    {
        private readonly string _username;
        private readonly Action<string> _emailUpdateCallback;
        private string _newEmail;

        public string NewEmail
        {
            get => _newEmail;
            set { _newEmail = value; OnPropertyChanged(); }
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
            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    var result = await client.RequestChangeEmailAsync(_username, NewEmail);
                    if (result.success)
                    {
                        MessageBox.Show(result.message, "Código Enviado");

                        var verifyVM = new VerifyChangesByCodeViewModel( 
                            VerifyChangesByCodeViewModel.VerificationMode.Email,
                            _username,
                            NewEmail,
                            _emailUpdateCallback
                        );

                        var verifyView = new VerifyChangesByCodeView { DataContext = verifyVM }; 

                        ExecuteClose(parameter);
                        verifyView.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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