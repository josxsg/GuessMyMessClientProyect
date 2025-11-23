using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    public class InviteByEmailViewModel : ViewModelBase
    {
        private string _targetEmail;
        private readonly string _matchId;

        public string TargetEmail
        {
            get => _targetEmail;
            set { _targetEmail = value; OnPropertyChanged(); }
        }

        public ICommand SendInviteCommand { get; }
        public ICommand CloseCommand { get; }

        public InviteByEmailViewModel() { }

        public InviteByEmailViewModel(string matchId)
        {
            _matchId = matchId;
            SendInviteCommand = new RelayCommand(ExecuteSendInvite);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private async void ExecuteSendInvite(object parameter)
        {
            if (string.IsNullOrWhiteSpace(TargetEmail))
            {
                MessageBox.Show(Lang.alertFieldsRequired, Lang.alertInputErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string myUsername = SessionManager.Instance.CurrentUsername;

            try
            {
                // CAMBIO: Usamos el Manager en lugar de crear un cliente nuevo
                await MatchmakingClientManager.Instance.InviteGuestByEmailAsync(myUsername, TargetEmail, _matchId);

                MessageBox.Show(Lang.alertInviteSentSuccess, Lang.alertSuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                if (parameter is Window window)
                {
                    window.Close();
                }
            }
            catch (FaultException<ServiceFaultDto> fault)
            {
                string msg = fault.Detail.ErrorType == ServiceErrorType.EmailAlreadyRegistered
                    ? "Este correo ya pertenece a un usuario registrado."
                    : fault.Detail.Message;

                MessageBox.Show(msg, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.alertUnknownErrorMessage, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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