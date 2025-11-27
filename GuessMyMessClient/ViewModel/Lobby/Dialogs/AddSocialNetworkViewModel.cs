using System;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.ViewModel;

namespace GuessMyMessClient.ViewModel.Lobby.Dialogs
{
    public class AddSocialNetworkViewModel : ViewModelBase
    {
        private string _userLink;
        private readonly Action<string> _onConfirm;

        // Propiedad enlazada al TextBox
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


        // El constructor sigue aceptando networkName para compatibilidad con ProfileViewModel
        public AddSocialNetworkViewModel(string networkName, string currentLink, Action<string> onConfirm)
        {
            _onConfirm = onConfirm;
            UserLink = currentLink ?? string.Empty;
            ConfirmCommand = new RelayCommand(ExecuteConfirm);
            CloseCommand = new RelayCommand(ExecuteClose);

        }

        private void ExecuteConfirm(object parameter)
        {
            // Validación simple
            if (string.IsNullOrWhiteSpace(UserLink))
            {
                MessageBox.Show("Por favor ingresa un enlace válido.", "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Invoca la acción de guardado en el ProfileViewModel
            _onConfirm?.Invoke(UserLink);

            // Cierra la ventana (el parámetro viene del XAML: AncestorType=Window)
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