using GuessMyMessClient.LobbyService;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.WaitingRoom
{
    public class WaitingRoomPublicMatchViewModel : WaitingRoomViewModelBase
    {
        public WaitingRoomPublicMatchViewModel(LobbyClientManager lobbyManager, SessionManager sessionManager)
            : base(lobbyManager, sessionManager)
        {
        }
    }
}