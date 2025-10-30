using GuessMyMessClient.LobbyService;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.WaitingRoom
{
    public class WaitingRoomPrivateMatchViewModel : WaitingRoomViewModelBase
    {
        public WaitingRoomPrivateMatchViewModel(LobbyClientManager lobbyManager, SessionManager sessionManager)
            : base(lobbyManager, sessionManager)
        {
        }
    }
}