using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using System.ServiceModel;
using GuessMyMessClient.ViewModel;

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
            try
            {
                using (var client = new UserProfileServiceClient())
                {
                    var result = await client.RequestChangeEmailAsync(_username, NewEmail);
                    if (result.Success )
                    {
                        MessageBox.Show(
                            result.Message,
                            Lang.alertCodeSentTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

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
                Console.WriteLine($"WCF Error requesting email change: {fexGeneral.Message}");
            }
            catch (EndpointNotFoundException ex)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Connection Error requesting email change: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Console.WriteLine($"Unknown Error requesting email change: {ex.Message}");
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
