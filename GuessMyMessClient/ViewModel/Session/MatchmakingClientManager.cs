using GuessMyMessClient.MatchmakingService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

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

        private MatchmakingClientManager()
        {
        }

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
                _client.Connect(username);
                _connectedUsername = username;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to matchmaking service: {ex.Message}");
                _client?.Abort();
                _client = null;
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending Disconnect signal: {ex.Message}");
                    }
                }

                if (_client.State != CommunicationState.Closed)
                {
                    try
                    {
                        _client.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing client, aborting: {ex.Message}");
                        _client.Abort();
                    }
                }
            }

            _client = null;
            _connectedUsername = null;
        }

        public async Task<OperationResultDto> CreateMatchAsync(LobbySettingsDto settings)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
                return new OperationResultDto { Success = false, Message = "Not connected to service." };

            try
            {
                return await _client.CreateMatchAsync(_connectedUsername, settings);
            }
            catch (Exception ex)
            {
                return new OperationResultDto { Success = false, Message = $"Error creating match: {ex.Message}" };
            }
        }

        public async Task<List<MatchInfoDto>> GetPublicMatchesAsync()
        {
            if (_client == null || _client.State != CommunicationState.Opened)
                return new List<MatchInfoDto>();

            try
            {
                var matchesArray = await _client.GetPublicMatchesAsync();
                return matchesArray?.ToList() ?? new List<MatchInfoDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting public matches: {ex.Message}");
                return new List<MatchInfoDto>();
            }
        }

        public async Task JoinPublicMatch(string matchId)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
            {
                OnMatchmakingFailed?.Invoke("Not connected to service.");
                return;
            }

            try
            {
                await _client.JoinPublicMatchAsync(_connectedUsername, matchId);
            }
            catch (Exception ex)
            {
                OnMatchmakingFailed?.Invoke($"Error joining public match: {ex.Message}");
            }
        }

        public async Task<OperationResultDto> JoinPrivateMatchAsync(string matchCode)
        {
            if (_client == null || _client.State != CommunicationState.Opened)
                return new OperationResultDto { Success = false, Message = "Not connected to service." };

            try
            {
                return await _client.JoinPrivateMatchAsync(_connectedUsername, matchCode);
            }
            catch (Exception ex)
            {
                return new OperationResultDto
                    { Success = false, Message = $"Error joining private match: {ex.Message}" };
            }
        }

        public void ReceiveMatchInvite(string fromUsername, string matchId)
        {
            OnMatchInviteReceived?.Invoke(fromUsername, matchId);
        }

        public void MatchUpdate(MatchInfoDto matchInfo)
        {
            OnMatchUpdated?.Invoke(matchInfo);
        }

        public void MatchJoined(string matchId, OperationResultDto result)
        {
            OnMatchJoinedSuccessfully?.Invoke(matchId, result);
        }

        public void MatchmakingFailed(string reason)
        {
            OnMatchmakingFailed?.Invoke(reason);
        }

        public void PublicMatchesListUpdated(MatchInfoDto[] publicMatches)
        {
            var matchesList = publicMatches?.ToList() ?? new List<MatchInfoDto>();

            Application.Current?.Dispatcher?.Invoke(() => { OnPublicMatchesListUpdated?.Invoke(matchesList); });
        }
    }
}