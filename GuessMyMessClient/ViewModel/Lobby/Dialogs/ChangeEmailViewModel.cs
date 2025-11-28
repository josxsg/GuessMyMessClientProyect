using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby.Dialogs;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using ServiceProfileFault = GuessMyMessClient.ProfileService.ServiceFaultDto;
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
                MessageBox.Show(
                    Lang.alertInvalidEmailFormat,
                    Lang.alertInputErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await client.RequestChangeEmailAsync(_username, NewEmail);

                if (result.Success)
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

                    client.Close();
                    isSuccess = true;
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
            catch (FaultException<ServiceProfileFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (FaultException)
            {
                MessageBox.Show(
                    Lang.alertServerErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertConnectionErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
