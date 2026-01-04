using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;
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
            get
            {
                return _targetEmail;
            }
            set
            {
                _targetEmail = value; 
                OnPropertyChanged();
            }
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
                ShowAlert(Lang.alertFieldsRequired, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            if (!InputValidator.IsValidEmail(TargetEmail))
            {
                ShowAlert(Lang.alertInvalidEmailFormat, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            string myUsername = SessionManager.Instance.CurrentUsername;

            try
            {
                await MatchmakingClientManager.Instance.InviteGuestByEmailAsync(myUsername, TargetEmail, _matchId);

                ShowAlert(Lang.alertInviteSentSuccess, Lang.alertSuccessTitle, MessageBoxImage.Information);

                if (parameter is Window window)
                {
                    window.Close();
                }
            }
            catch (FaultException<GuessMyMessClient.MatchmakingService.ServiceFaultDto> fex)
            {
                ShowServiceError((GuessMyMessClient.AuthService.ServiceErrorType)fex.Detail.ErrorType);
            }
            catch (Exception ex)
            {
                HandleException(ex);
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