using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GuessMyMessClient.Model;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel;
using ServiceProfileFault = GuessMyMessClient.ProfileService.ServiceFaultDto;

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
                Task.Run(() => LoadAvatarsAsync());
            }
        }

        private async Task LoadAvatarsAsync()
        {
            var client = new UserProfileServiceClient();
            bool isSuccess = false;

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
                            ImageData = avatarDto.AvatarData
                        });
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var avatar in tempAvatars)
                    {
                        avatar.ImageSource = ConvertByteToImage(avatar.ImageData);
                    }

                    AvailableAvatars = new ObservableCollection<AvatarModel>(tempAvatars);
                    SelectedAvatar = AvailableAvatars.FirstOrDefault(a => a.Id == _currentAvatarId)
                                     ?? AvailableAvatars.FirstOrDefault();
                });

                client.Close();
                isSuccess = true;
            }
            catch (FaultException<ServiceProfileFault> fex)
            {
                ShowError(fex.Detail.Message);
            }
            catch (FaultException)
            {
                ShowError(Lang.alertAvatarLoadServerError);
            }
            catch (Exception ex) when (ex is EndpointNotFoundException || ex is TimeoutException || ex is CommunicationException)
            {
                ShowError(Lang.alertConnectionErrorMessage);
            }
            catch
            {
                ShowError(Lang.alertAvatarLoadUnknownError);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    message,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
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

            try
            {
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
            catch
            {
                return null;
            }
        }
    }
}
