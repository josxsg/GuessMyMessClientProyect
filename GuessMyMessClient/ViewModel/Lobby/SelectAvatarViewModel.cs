using System;
using System.Collections.Generic;
using System.Linq;
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
                LoadAvatars();
            }
        }

        private async void LoadAvatars()
        {
            await LoadAvatarsAsync();
        }

        private async Task LoadAvatarsAsync()
        {
            var client = new UserProfileServiceClient();
            try
            {
                var serverAvatars = await client.GetAvailableAvatarsAsync();

                var tempAvatars = new ObservableCollection<AvatarModel>();
                foreach (var avatarDto in serverAvatars)
                {
                    tempAvatars.Add(new AvatarModel
                    {
                        Id = avatarDto.idAvatar,
                        Name = avatarDto.avatarName,
                        ImageData = avatarDto.avatarData,
                        ImageSource = ConvertByteToImage(avatarDto.avatarData)
                    });
                }

                AvailableAvatars = tempAvatars;
                SelectedAvatar = AvailableAvatars.FirstOrDefault(a => a.Id == _currentAvatarId) ?? AvailableAvatars.FirstOrDefault();

                client.Close();
            }
            catch (FaultException ex)
            {
                MessageBox.Show($"Error del servidor al cargar avatares: {ex.Message}", "Error WCF");
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al cargar avatares: {ex.Message}", "Error Crítico");
                client.Abort();
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