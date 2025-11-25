using GuessMyMessClient.MatchmakingService;
using GuessMyMessClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

using ServiceMatchFault = GuessMyMessClient.MatchmakingService.ServiceFaultDto;

namespace GuessMyMessClient.ViewModel.Session
{
    public class MatchmakingClientManager : IMatchmakingServiceCallback
    {
        public static MatchmakingClientManager Instance { get; private set; }

        private MatchmakingServiceClient _client;
        private string _connectedUsername;

        public event Action<string, string> OnMatchInviteReceived;
        public event Action<MatchInfoDto> OnMatchUpdated;
        public event Action<string, OperationResultDto> OnMatchJoinedSuccessfully;
        public event Action<string> OnMatchmakingFailed;
        public event Action<List<MatchInfoDto>> OnPublicMatchesListUpdated;

        private MatchmakingClientManager() { }

        public static void Initialize()
        {
            if (Instance == null)
            {
                Instance = new MatchmakingClientManager();
            }
        }

        public bool Connect(string username)
        {
            try
            {
                Disconnect();

                InstanceContext context = new InstanceContext(this);
                _client = new MatchmakingServiceClient(context);
                _client.Open();

                _client.InnerChannel.Faulted += Channel_Faulted;
                _client.InnerChannel.Closed += Channel_Closed;

                _client.Connect(username);
                _connectedUsername = username;
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error); 
                CleanupConnection();
                return false;
            }
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                if (_client.State == CommunicationState.Opened && !string.IsNullOrEmpty(_connectedUsername))
                {
                    try
                    {
                        _client.Disconnect(_connectedUsername);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            Lang.alertUnknownErrorMessage,
                            Lang.alertErrorTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                CleanupConnection();
            }
        }

        public async Task InviteGuestByEmailAsync(string inviterUsername, string targetEmail, string matchId)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                throw new Exception(Lang.alertConnectionErrorTitle);
            }

            await _client.InviteGuestByEmailAsync(inviterUsername, targetEmail, matchId);
        }

        private void CleanupConnection()
        {
            if (_client != null)
            {
                try
                {
                    _client.InnerChannel.Faulted -= Channel_Faulted;
                    _client.InnerChannel.Closed -= Channel_Closed;
                }
                catch { }

                try
                {
                    if (_client.State != CommunicationState.Faulted)
                    {
                        _client.Close();
                    }
                    else
                    {
                        _client.Abort();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        Lang.alertUnknownErrorMessage,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    _client.Abort();
                }
                finally
                {
                    _client = null;
                    _connectedUsername = null;
                }
            }
        }

        public async Task<OperationResultDto> CreateMatchAsync(LobbySettingsDto settings)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                return new OperationResultDto { Success = false, Message = Lang.alertConnectionErrorTitle };
            }

            try
            {
                return await _client.CreateMatchAsync(_connectedUsername, settings);
            }
            catch (FaultException<ServiceMatchFault> fex)
            {
                return new OperationResultDto { Success = false, Message = fex.Detail.Message };
            }
            catch (Exception)
            {
                return new OperationResultDto { Success = false, Message = Lang.alertConnectionErrorTitle };
            }
        }

        public async Task<List<MatchInfoDto>> GetPublicMatchesAsync()
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                return new List<MatchInfoDto>();
            }

            try
            {
                var matchesArray = await _client.GetPublicMatchesAsync();
                return matchesArray?.ToList() ?? new List<MatchInfoDto>();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return new List<MatchInfoDto>();
            }
        }

        public void JoinPublicMatch(string matchId)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                OnMatchmakingFailed?.Invoke(Lang.alertConnectionErrorTitle);
                return;
            }

            try
            {
                _client.JoinPublicMatch(_connectedUsername, matchId);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task<OperationResultDto> JoinPrivateMatchAsync(string matchCode)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                return new OperationResultDto { Success = false, Message = Lang.alertConnectionErrorTitle };
            }

            try
            {
                return await _client.JoinPrivateMatchAsync(_connectedUsername, matchCode);
            }
            catch (FaultException<ServiceMatchFault> fex)
            {
                return new OperationResultDto { Success = false, Message = fex.Detail.Message };
            }
            catch (Exception)
            {
                return new OperationResultDto { Success = false, Message = Lang.alertConnectionErrorTitle };
            }
        }

        public void ReceiveMatchInvite(string fromUsername, string matchId)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnMatchInviteReceived?.Invoke(fromUsername, matchId);
            });
        }

        public void MatchUpdate(MatchInfoDto matchInfo)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnMatchUpdated?.Invoke(matchInfo);
            });
        }

        public void MatchJoined(string matchId, OperationResultDto result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnMatchJoinedSuccessfully?.Invoke(matchId, result);
            });
        }

        public void MatchmakingFailed(string reason)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnMatchmakingFailed?.Invoke(reason);
            });
        }

        public void PublicMatchesListUpdated(MatchInfoDto[] publicMatches)
        {
            var matchesList = publicMatches?.ToList() ?? new List<MatchInfoDto>();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPublicMatchesListUpdated?.Invoke(matchesList);
            });
        }

        private void Channel_Faulted(object sender, EventArgs e)
        {
            CleanupConnection();
            OnMatchmakingFailed?.Invoke("Connection lost (Channel Faulted).");
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            CleanupConnection();
        }
    }
}
