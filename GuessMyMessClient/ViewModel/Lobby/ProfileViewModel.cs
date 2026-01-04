using GuessMyMessClient.ProfileService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.Lobby.Dialogs;
using GuessMyMessClient.ViewModel;
using GuessMyMessClient.ViewModel.Lobby.Dialogs;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly UserProfileDto _profileData;

        public string FirstName
        {
            get
            {
                return _profileData.FirstName;
            }
            set
            {
                if (_profileData.FirstName != value)
                {
                    _profileData.FirstName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastName
        {
            get
            {
                return _profileData.LastName;
            }
            set
            {
                if (_profileData.LastName != value)
                {
                    _profileData.LastName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username => _profileData?.Username;

        public string Email
        {
            get
            {
                return _profileData?.Email;
            }
            set
            {
                if (_profileData != null && _profileData.Email != value)
                {
                    _profileData.Email = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMale => _profileData?.GenderId == 1;
        public bool IsFemale => _profileData?.GenderId == 2;
        public bool IsNonBinary => _profileData?.GenderId == 3;

        public ICommand ChangeEmailCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand AddDiscordCommand { get; }
        public ICommand AddTwitterCommand { get; }
        public ICommand AddInstagramCommand { get; }
        public ICommand AddTiktokCommand { get; }
        public ICommand AddTwitchCommand { get; }


        public ProfileViewModel(UserProfileDto initialProfileData)
        {
            _profileData = initialProfileData ?? throw new ArgumentNullException(nameof(initialProfileData));
            if (_profileData.socialNetworks == null)
            {
                _profileData.socialNetworks = new SocialNetworkDto[0];
            }
            ChangeEmailCommand = new RelayCommand(ExecuteChangeEmail);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            AddDiscordCommand = new RelayCommand(ExecuteAddSocialNetwork);
            AddTwitterCommand = new RelayCommand(ExecuteAddSocialNetwork);
            AddInstagramCommand = new RelayCommand(ExecuteAddSocialNetwork);
            AddTiktokCommand = new RelayCommand(ExecuteAddSocialNetwork);
            AddTwitchCommand = new RelayCommand(ExecuteAddSocialNetwork);

        }

        private void ExecuteAddSocialNetwork(object parameter)
        {
            if (parameter is string networkName)
            {
                var existingNetwork = _profileData.socialNetworks
                    .FirstOrDefault(s => s.NetworkType.Equals(networkName, StringComparison.InvariantCultureIgnoreCase));

                string currentLink = existingNetwork?.UserLink;

                var dialogVM = new AddSocialNetworkViewModel(networkName, currentLink, (enteredLink) =>
                {
                    SaveSocialNetworkToServer(networkName, enteredLink);
                });

                var dialogView = new AddSocialNetworkView
                {
                    DataContext = dialogVM,
                    Owner = Application.Current.MainWindow
                };

                dialogView.ShowDialog();
            }
        }

        private async Task SaveSocialNetworkToServer(string networkName, string userLink)
        {
            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                var socialDto = new SocialNetworkDto
                {
                    NetworkType = networkName,
                    UserLink = userLink
                };

                OperationResultDto result = await client.AddOrUpdateSocialNetworkAsync(_profileData.Username, socialDto);

                if (result.Success)
                {
                    ShowAlert(Lang.alertProfileUpdateSuccess, Lang.alertSuccessTitle, MessageBoxImage.Information);

                    UpdateLocalSocialNetworkList(networkName, userLink);

                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    ShowServiceError((AuthService.ServiceErrorType)result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private void UpdateLocalSocialNetworkList(string networkName, string newLink)
        {
            var socialList = _profileData.socialNetworks.ToList();

            var existingItem = socialList.FirstOrDefault(s => s.NetworkType == networkName);

            if (existingItem != null)
            {
                existingItem.UserLink = newLink;
            }
            else
            {
                socialList.Add(new SocialNetworkDto { NetworkType = networkName, UserLink = newLink });
            }

            _profileData.socialNetworks = socialList.ToArray();
        }
        private async void ExecuteSaveProfile(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                ShowAlert(Lang.alertProfileMandatoryFields, Lang.alertInputErrorTitle, MessageBoxImage.Warning);
                return;
            }

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                OperationResultDto result = await client.UpdateProfileAsync(_profileData.Username, _profileData);

                if (result.Success)
                {
                    ShowAlert(Lang.alertProfileUpdateSuccess, Lang.alertSuccessTitle, MessageBoxImage.Information);
                    client.Close();
                    isSuccess = true;
                }
                else
                {
                    ShowServiceError((AuthService.ServiceErrorType)result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }

        private void ExecuteChangeEmail(object parameter)
        {
            var changeEmailVM = new ChangeEmailViewModel(_profileData.Username, (newEmail) =>
            {
                Email = newEmail;
            });

            var changeEmailView = new ChangeEmailView
            {
                DataContext = changeEmailVM
            };
            changeEmailView.ShowDialog();
        }

        private async void ExecuteChangePassword(object parameter)
        {
            ShowAlert(Lang.alertPasswordChangeCodeInfo, Lang.alertPasswordChangeTitle, MessageBoxImage.Information);

            var client = new UserProfileServiceClient();
            bool isSuccess = false;

            try
            {
                var result = await client.RequestChangePasswordAsync(_profileData.Username);

                if (result.Success)
                {
                    var changePasswordVM = new ChangePasswordViewModel(_profileData.Username);
                    var changePasswordView = new ChangePasswordView
                    {
                        DataContext = changePasswordVM
                    };

                    client.Close();
                    isSuccess = true;

                    changePasswordView.ShowDialog();
                }
                else
                {
                    ShowServiceError((AuthService.ServiceErrorType)result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (!isSuccess && client.State != CommunicationState.Closed)
                {
                    client.Abort();
                }
            }
        }
    }
}