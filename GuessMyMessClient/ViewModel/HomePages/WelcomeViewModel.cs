using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class WelcomeViewModel : ViewModelBase
    {
        public ICommand SignUpCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand ContinueAsGuestCommand { get; }

        public WelcomeViewModel()
        {
            SignUpCommand = new RelayCommand(SignUp);
            LoginCommand = new RelayCommand(Login);
            ContinueAsGuestCommand = new RelayCommand(ContinueAsGuest);
        }

        private void SignUp(object parameter)
        {
            // Lógica para abrir la ventana de registro
            MessageBox.Show("Navegando a la página de Registro...");
        }

        private void Login(object parameter)
        {
            // Lógica para abrir la ventana de inicio de sesión
            MessageBox.Show("Navegando a la página de Inicio de Sesión...");
        }

        private void ContinueAsGuest(object parameter)
        {
            // Lógica para continuar al lobby como invitado
            MessageBox.Show("Continuando como invitado...");
        }
    }
}
