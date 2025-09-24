using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace GuessMyMessClient.ViewModel
{
    public class EditAvatarViewModel : ViewModelBase
    {
        // Propiedad para el avatar seleccionado (si es necesario)
        // Por ahora es un placeholder, puedes adaptar el tipo de dato.
        private object _selectedAvatar;
        public object SelectedAvatar
        {
            get { return _selectedAvatar; }
            set
            {
                _selectedAvatar = value;
            }
        }

        // Comando que se enlazará al botón "CONFIRMAR"
        public ICommand ConfirmSelectionCommand { get; }

        public EditAvatarViewModel()
        {
            // Inicializamos el comando y le decimos qué método debe ejecutar
            ConfirmSelectionCommand = new RelayCommand(ConfirmSelection);
        }

        private void ConfirmSelection(object parameter)
        {
            // Aquí puedes implementar la lógica para guardar el avatar seleccionado
            // Por ejemplo, guardarlo en la configuración del usuario o enviarlo al servidor.
            MessageBox.Show("¡Avatar seleccionado! (Lógica del ViewModel)");

            // Ejemplo de cómo podrías cerrar la ventana emergente:
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}