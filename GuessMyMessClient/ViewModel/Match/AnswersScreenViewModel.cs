using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace GuessMyMessClient.ViewModel.Match
{
    public class AnswersScreenViewModel : ViewModelBase
    {
        private string _drawingArtistName;
        public string DrawingArtistName
        {
            get => _drawingArtistName;
            set { _drawingArtistName = value; OnPropertyChanged(nameof(DrawingArtistName)); }
        }

        public ObservableCollection<PlayerViewModel> PlayerList { get; set; }
        public ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; }

        private string _newChatMessage;
        public string NewChatMessage
        {
            get => _newChatMessage;
            set
            {
                _newChatMessage = value;
                OnPropertyChanged(nameof(NewChatMessage));
            }
        }

        private ImageSource _currentDrawing;
        public ImageSource CurrentDrawing
        {
            get => _currentDrawing;
            set { _currentDrawing = value; OnPropertyChanged(nameof(CurrentDrawing)); }
        }

        private string _currentDisplayedGuess;
        public string CurrentDisplayedGuess
        {
            get => _currentDisplayedGuess;
            set { _currentDisplayedGuess = value; OnPropertyChanged(nameof(CurrentDisplayedGuess)); }
        }

        public ICommand SendMessageCommand { get; }

        public AnswersScreenViewModel()
        {
        }

        private void OnSendChatMessage(object parameter)
        {
        }

        private bool CanSendChatMessage(object parameter)
        {
            return !string.IsNullOrEmpty(NewChatMessage);
        }

        private void LoadDesignTimeData()
        {
        }
    }

    public class PlayerViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }
    }

    public class ChatMessageViewModel : ViewModelBase
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }
}
