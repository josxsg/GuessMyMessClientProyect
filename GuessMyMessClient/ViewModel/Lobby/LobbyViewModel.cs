using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.ProfileService;
using GuessMyMessClient.View.Lobby;
using GuessMyMessClient.ViewModel.Session;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.Model;
using System.Collections.ObjectModel;
using System.IO;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class LobbyViewModel : ViewModelBase
    {
        private ObservableCollection<AvatarModel> _availableAvatars;
        public event Action<AvatarModel> AvatarSelected;
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

        private UserProfileDto _userProfileData;

        // Propiedad expuesta que contiene todos los datos cargados desde el servidor
        public UserProfileDto UserProfileData
        {
            get => _userProfileData;
            set { _userProfileData = value; OnPropertyChanged(); }
        }

        private string _username;
        // Propiedades de la UI
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }
        private BitmapImage _userAvatar;
        public BitmapImage UserAvatar
        {
            get => _userAvatar;
            set { _userAvatar = value; OnPropertyChanged(); }
        }

        // PROPIEDADES DE COMANDOS REQUERIDAS POR EL XAML
        public ICommand SettingsCommand { get; }
        public ICommand FriendsCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand CreateGameCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand SelectAvatarCommand { get; }
        // ... (otros comandos como Play, CreateGame, etc.) ...

        public LobbyViewModel()
        {
            // Inicialización de Comandos (FIX DE BINDING)
            SettingsCommand = new RelayCommand(ExecuteSettings);
            FriendsCommand = new RelayCommand(ExecuteFriends);
            ChatCommand = new RelayCommand(ExecuteChat);
            PlayCommand = new RelayCommand(ExecutePlay);
            CreateGameCommand = new RelayCommand(ExecuteCreateGame);
            EditProfileCommand = new RelayCommand(ExecuteEditProfile);
            SelectAvatarCommand = new RelayCommand(ExecuteSelectAvatar);
            AvailableAvatars = new ObservableCollection<AvatarModel>();

            LoadUserProfileAsync();
        }

        // --- IMPLEMENTACIONES MÍNIMAS (STUBS) ---
        private void ExecuteSettings(object param) { new ConfigurationView().ShowDialog(); }
        private void ExecuteFriends(object param) { new FriendsView().ShowDialog(); }
        private void ExecuteChat(object param) { MessageBox.Show("Chat aún no implementado."); }
        private void ExecutePlay(object param) { MessageBox.Show("Navegando a Partidas Públicas..."); }
        private void ExecuteCreateGame(object param) { MessageBox.Show("Creando Partida..."); }



        private async void LoadUserProfileAsync()
        {
            if (SessionManager.Instance.IsLoggedIn)
            {
                using (var client = new UserProfileServiceClient())
                {
                    try
                    {
                        // 1. Obtener DTO del perfil (Nombre, Email, etc.)
                        UserProfileDto profileData =
                            await client.GetUserProfileAsync(SessionManager.Instance.CurrentUsername);
                        
                        if (profileData != null)
                        {
                            UserProfileData = profileData; // Asigna el DTO
                            Username = profileData.Username; // Asigna el nombre de usuario
                            //UserAvatar = ConvertByteToImage(profileData.Avatar); // Asigna el avatar
                            var loadedAvatars = await Task.Run(() =>
                            {
                                using (client)
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
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al cargar el perfil: {ex.Message}", "Error de Carga");
                    }
                }
            }
        }

        private void ExecuteEditProfile(object parameter)
        {
            if (UserProfileData == null)
            {
                MessageBox.Show("Aún no se ha cargado la información del usuario.", "Error de Carga");
                return;
            }

            var profileView = new ProfileView();

            // PASAR EL DTO COMPLETO para que ProfileViewModel lo maneje
            profileView.DataContext = new ProfileViewModel(UserProfileData);

            profileView.ShowDialog();
        }

        private void ExecuteSelectAvatar(object parameter)
        {
            // Lógica para abrir el selector de avatar si el usuario hace clic en el icono del 
            // (Similar a la implementacion en SignUpViewModel
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

        // Asegúrate de que el resto de los comandos de Lobby (Play, CreateGame) están implementados
    }
}
