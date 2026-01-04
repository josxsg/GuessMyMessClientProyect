using System;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    public class AddSocialNetworkViewModel : ViewModelBase
    {
        private string _userLink;
        private readonly Action<string> _onConfirm;

        public string UserLink
        {
            get => _userLink;
            set
            {
                _userLink = value;
                OnPropertyChanged();
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CloseCommand { get; }

        public AddSocialNetworkViewModel(string networkName, string currentLink, Action<string> onConfirm)
        {
            _onConfirm = onConfirm;
            UserLink = currentLink ?? string.Empty;
            ConfirmCommand = new RelayCommand(ExecuteConfirm);
            CloseCommand = new RelayCommand(ExecuteClose);

        }

        private void ExecuteConfirm(object parameter)
        {
            if (string.IsNullOrWhiteSpace(UserLink))
            {
                MessageBox.Show(Lang.alertEmptyLink, Lang.alertEmptyTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _onConfirm?.Invoke(UserLink);

            if (parameter is Window window)
            {
                window.Close();
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