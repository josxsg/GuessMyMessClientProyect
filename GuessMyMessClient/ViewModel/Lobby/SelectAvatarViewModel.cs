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
using System.ServiceModel;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class SelectAvatarViewModel : ViewModelBase
    {
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
            set
            {
                _selectedAvatar = value;
                OnPropertyChanged();
                // Fuerza la re-evaluación del botón Confirmar.
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand ConfirmSelectionCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SelectAvatarItemCommand { get; } // COMANDO PARA SELECCIONAR ÍTEM

        public SelectAvatarViewModel()
        {
            AvailableAvatars = new ObservableCollection<AvatarModel>();
            ConfirmSelectionCommand = new RelayCommand(ExecuteConfirmSelection, CanExecuteConfirmSelection);
            CloseCommand = new RelayCommand(CloseWindow);
            SelectAvatarItemCommand = new RelayCommand(ExecuteSelectAvatarItem); // Implementación

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadAvatarsAsync();
            }
        }

        // LÓGICA DE SELECCIÓN DE ÍTEM
        private void ExecuteSelectAvatarItem(object parameter)
        {
            if (parameter is AvatarModel avatar)
            {
                SelectedAvatar = avatar;
            }
        }

        private bool CanExecuteConfirmSelection(object parameter) => SelectedAvatar != null;

        private void ExecuteConfirmSelection(object parameter)
        {
            AvatarSelected?.Invoke(SelectedAvatar);
            CloseWindow(parameter);
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }

        // CORRECCIÓN DEADLOCK Y CONGELAMIENTO
        private async void LoadAvatarsAsync()
        {
            try
            {
                // 1. Ejecutar TODA la lógica pesada (WCF, ToList(), conversión) en el background.
                var loadedAvatars = await Task.Run(() =>
                {
                    using (UserProfileServiceClient client = new UserProfileServiceClient())
                    {
                        // Obtener el Array/List del servidor
                        List<ProfileService.AvatarDto> serverAvatars = client.GetAvailableAvatars().ToList();

                        var tempAvatars = new ObservableCollection<AvatarModel>();

                        foreach (var avatarDto in serverAvatars)
                        {
                            var avatarModel = new AvatarModel
                            {
                                Id = avatarDto.idAvatar,
                                Name = avatarDto.avatarName,
                                ImageData = avatarDto.avatarData,
                                ImageSource = ConvertByteToImage(avatarDto.avatarData)
                            };
                            tempAvatars.Add(avatarModel);
                        }
                        return tempAvatars;
                    }
                }).ConfigureAwait(false); // Permite que la continuación se ejecute en el hilo de trabajo.

                // 2. Actualizar la UI de forma segura con el Dispatcher.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableAvatars = loadedAvatars;
                    // Seleccionar el primer avatar por defecto
                    if (AvailableAvatars.Any())
                    {
                        SelectedAvatar = AvailableAvatars.First();
                    }
                });
            }
            // ... (Se mantienen los catch blocks para manejo de excepciones) ...
            catch (FaultException ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error de Lógica del Servidor: {ex.Message}", "Error WCF");
                });
            }
            catch (EndpointNotFoundException)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Error de Conexión: El servidor no está ejecutándose.", "Error Crítico");
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error inesperado: {ex.Message}", "Error General");
                });
            }
        }

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
                image.Freeze();
            }
            return image;
        }
    }
}