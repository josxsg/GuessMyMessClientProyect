using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.SocialService;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class FriendProfileViewModel : ViewModelBase
    {
        private string _targetUsername;

        private string _usernameTitle;
        public string UsernameTitle 
        {
            get
            {
                return _usernameTitle;
            }
            set
            {
                _usernameTitle = value; 
                OnPropertyChanged();
            }
        }

        private string _firstName;
        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }

        private string _lastName;
        public string LastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }

        private string _email;
        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        private bool _isMale;
        public bool IsMale
        {
            get
            {
                return _isMale;
            }
            set
            {
                _isMale = value;
                OnPropertyChanged();
            }
        }

        private bool _isFemale;
        public bool IsFemale
        {
            get
            {
                return _isFemale;
            }
            set
            {
                _isFemale = value;
                OnPropertyChanged();
            }
        }

        private bool _isNonBinary;
        public bool IsNonBinary
        {
            get
            {
                return _isNonBinary;
            }
            set
            {
                _isNonBinary = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FriendSocialNetworkModel> SocialNetworks { get; set; }

        public FriendProfileViewModel(string username)
        {
            _targetUsername = username;
            UsernameTitle = username;
            SocialNetworks = new ObservableCollection<FriendSocialNetworkModel>();

            Task.Run(() => LoadProfileAsync());
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await SocialClientManager.Instance.Client.GetFriendProfileAsync(_targetUsername);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FirstName = profile.FirstName;
                    LastName = profile.LastName;
                    Email = profile.Email;

                    IsMale = profile.GenderId == 1;
                    IsFemale = profile.GenderId == 2;
                    IsNonBinary = profile.GenderId == 3;

                    PrepareSocialNetworksDisplay(profile.SocialNetworks);
                });
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("No se pudo cargar el perfil.", Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void PrepareSocialNetworksDisplay(SocialNetworkDto[] fetchedNetworks)
        {
            SocialNetworks.Clear();

            var supportedNetworks = new[]
            {
                new { DbName = "Discord", Icon = "/Resources/Images/discord.png" },
                new { DbName = "X", Icon = "/Resources/Images/twitterX.png" }, 
                new { DbName = "Instagram", Icon = "/Resources/Images/instagram.png" },
                new { DbName = "TikTok", Icon = "/Resources/Images/tiktok.png" },
                new { DbName = "Twitch", Icon = "/Resources/Images/twitch.png" }
            };

            foreach (var net in supportedNetworks)
            {
                var userNet = fetchedNetworks?.FirstOrDefault(n => n.NetworkType.Equals(net.DbName, StringComparison.OrdinalIgnoreCase));

                string displayText = (userNet != null && !string.IsNullOrWhiteSpace(userNet.UserLink))
                                     ? userNet.UserLink
                                     : Lang.globalNoLinked;

                string textColor = (userNet != null && !string.IsNullOrWhiteSpace(userNet.UserLink))
                                   ? "#333333"
                                   : "#999999";

                SocialNetworks.Add(new FriendSocialNetworkModel
                {
                    NetworkName = net.DbName,
                    IconPath = net.Icon,
                    DisplayValue = displayText,
                    TextColor = textColor
                });
            }
        }
    }

    public class FriendSocialNetworkModel
    {
        public string NetworkName { get; set; }
        public string IconPath { get; set; }
        public string DisplayValue { get; set; }
        public string TextColor { get; set; }
    }
}