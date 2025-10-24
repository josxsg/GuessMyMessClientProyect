using System;
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
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using System.Collections.Generic;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class SelectAvatarViewModel : ViewModelBase
    {
        private readonly int _currentAvatarId;
        public event Action<AvatarModel> AvatarSelected;

        private ObservableCollection<AvatarModel> _availableAvatars;
        public ObservableCollection<AvatarModel> AvailableAvatars
        {
            get
            {
                return _availableAvatars;
            }
            set
            {
                if (_availableAvatars != value)
                {
                    _availableAvatars = value;
                    OnPropertyChanged();
                }
            }
        }

        private AvatarModel _selectedAvatar;
        public AvatarModel SelectedAvatar
        {
            get
            {
                return _selectedAvatar;
            }
            set
            {
                if (_selectedAvatar != value)
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
            using (var client = new UserProfileServiceClient())
            {
                try
                {
                    var serverAvatars = await client.GetAvailableAvatarsAsync();

                    var tempAvatars = new List<AvatarModel>();
                    if (serverAvatars != null)
                    {
                        foreach (var avatarDto in serverAvatars)
                        {
                            tempAvatars.Add(new AvatarModel
                            {
                                Id = avatarDto.IdAvatar,
                                Name = avatarDto.AvatarName,
                                ImageData = avatarDto.AvatarData,
                                ImageSource = ConvertByteToImage(avatarDto.AvatarData)
                            });
                        }
                    }

                    AvailableAvatars = new ObservableCollection<AvatarModel>(tempAvatars);

                    SelectedAvatar = AvailableAvatars.FirstOrDefault(a => a.Id == _currentAvatarId)
                                     ?? AvailableAvatars.FirstOrDefault();
                }
                catch (FaultException fexGeneral)
                {
                    MessageBox.Show(
                        Lang.alertAvatarLoadServerError,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"WCF Error loading avatars: {fexGeneral.Message}");
                }
                catch (EndpointNotFoundException ex)
                {
                    MessageBox.Show(
                        Lang.alertConnectionErrorMessage,
                        Lang.alertConnectionErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Connection Error loading avatars: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        Lang.alertAvatarLoadUnknownError,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine($"Error crítico al cargar avatares: {ex.Message}");
                }
            }
        }

        private void ExecuteSelectAvatarItem(object parameter)
        {
            if (parameter is AvatarModel avatar)
            {
                SelectedAvatar = avatar;
            }
        }

        private bool CanExecuteConfirmSelection(object parameter)
        {
            return SelectedAvatar != null;
        }

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
            else
            {
                var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                activeWindow?.Close();
            }
        }

        public static BitmapImage ConvertByteToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

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
            }
            image.Freeze();
            return image;
        }
    }
}
