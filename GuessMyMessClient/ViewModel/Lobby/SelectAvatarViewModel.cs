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
        private readonly int _currentAvatarId;
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
                if (_selectedAvatar != null)
                {
                    _selectedAvatar.IsSelected = false; 
                }

                _selectedAvatar = value;

                if (_selectedAvatar != null)
                {
                    _selectedAvatar.IsSelected = true; 
                }

                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand ConfirmSelectionCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SelectAvatarItemCommand { get; } 

        public SelectAvatarViewModel(int currentAvatarId = 1)
        {
            _currentAvatarId = currentAvatarId; 
            AvailableAvatars = new ObservableCollection<AvatarModel>();
            ConfirmSelectionCommand = new RelayCommand(ExecuteConfirmSelection, CanExecuteConfirmSelection);
            CloseCommand = new RelayCommand(CloseWindow);
            SelectAvatarItemCommand = new RelayCommand(ExecuteSelectAvatarItem);

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadAvatarsAsync();
            }
        }

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

        private async void LoadAvatarsAsync()
        {
            try
            {
                var loadedAvatars = await Task.Run(() =>
                {
                    using (UserProfileServiceClient client = new UserProfileServiceClient())
                    {
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
                }).ConfigureAwait(false); 

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableAvatars = loadedAvatars;
                    SelectedAvatar = AvailableAvatars.FirstOrDefault(a => a.Id == _currentAvatarId);

                    if (SelectedAvatar == null && AvailableAvatars.Any())
                    {
                        SelectedAvatar = AvailableAvatars.First();
                    }
                });
            }
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