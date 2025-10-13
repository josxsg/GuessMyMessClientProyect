using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.Model;
using GuessMyMessClient.ProfileService;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class SelectAvatarViewModel : ViewModelBase
    {
        // Propiedad para comunicar el avatar seleccionado de vuelta al ViewModel padre
        public event Action<AvatarModel> AvatarSelected;

        private ObservableCollection<AvatarModel> _availableAvatars;
        public ObservableCollection<AvatarModel> AvailableAvatars
        {
            get => _availableAvatars;
            set { _availableAvatars = value; OnPropertyChanged(); }
        }

        private AvatarModel _selectedAvatar;
        public AvatarModel SelectedAvatar
        {
            get => _selectedAvatar;
            set { _selectedAvatar = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmSelectionCommand { get; }
        public ICommand CloseCommand { get; }

        public SelectAvatarViewModel()
        {
            AvailableAvatars = new ObservableCollection<AvatarModel>();
            ConfirmSelectionCommand = new RelayCommand(ExecuteConfirmSelection, CanExecuteConfirmSelection);
            CloseCommand = new RelayCommand(CloseWindow);
            // VERIFICACIÓN CLAVE: Solo llama a la carga si NO estamos en el diseñador
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadAvatarsAsync();
            }
        }

        private bool CanExecuteConfirmSelection(object parameter) => SelectedAvatar != null;

        private void ExecuteConfirmSelection(object parameter)
        {
            // Notifica al ViewModel padre (SignUpViewModel) cuál fue el avatar elegido
            AvatarSelected?.Invoke(SelectedAvatar);

            // Cierra el diálogo
            CloseWindow(parameter);
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }

        // **Nueva Lógica: Carga de Avatares del Servidor**
        private async void LoadAvatarsAsync()
        {
            // Nota: Este método WCF (GetAvailableAvatarsAsync) debe existir en su IUserProfileService
            IUserProfileService client = new UserProfileServiceClient();

            try
            {
                // 1. LLAMADA ROBUSTA: Usamos Task.Run para llamar a la versión síncrona
                // del proxy WCF y evitar el error de nombre del método asíncrono.
                // El tipo de retorno es la lista o array de DTOs del SERVIDOR.
                var serverAvatars = await Task.Run(() => client.getAvailableAvatars());

                // 2. CREACIÓN DE LA COLECCIÓN LOCAL: 
                // Esta colección es de nuestro tipo local, AvatarModel.
                var loadedAvatars = new ObservableCollection<AvatarModel>();

                // 3. MAPEANDO DE DTO (Servidor) A MODEL (Cliente)
                // Recorremos el Array/List que viene del servidor y lo convertimos a nuestro tipo local.
                foreach (var avatarDto in serverAvatars)
                {
                    var avatarModel = new AvatarModel
                    {
                        // Mapeo: DTO de WCF (avatarDto.idAvatar) -> Model Local (AvatarModel.Id)
                        Id = avatarDto.idAvatar,
                        Name = avatarDto.avatarName,
                        ImageData = avatarDto.avatarData,
                        // Conversión de byte[] a BitmapImage
                        ImageSource = ConvertByteToImage(avatarDto.avatarData)
                    };
                    loadedAvatars.Add(avatarModel);
                }

                AvailableAvatars = loadedAvatars;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar avatares: {ex.Message}", "Error de Conexión");
            }
        }

        // Método de Conversión: byte[] a BitmapImage para mostrar en XAML
        public static BitmapImage ConvertByteToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
                image.Freeze(); // Importante para la multihilo
            }
            return image;
        }
    }
}