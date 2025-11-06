using GuessMyMessClient.ViewModel;
using System.ComponentModel;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Match
{
    public class EndOfMatchViewModel : ViewModelBase
    {
        private string _winnerName;
        public string WinnerName
        {
            get => _winnerName;
            set { _winnerName = value; OnPropertyChanged(nameof(WinnerName)); }
        }

        private string _firstPlacePlayerName;
        public string FirstPlacePlayerName
        {
            get => _firstPlacePlayerName;
            set { _firstPlacePlayerName = value; OnPropertyChanged(nameof(FirstPlacePlayerName)); }
        }

        private string _secondPlacePlayerName;
        public string SecondPlacePlayerName
        {
            get => _secondPlacePlayerName;
            set { _secondPlacePlayerName = value; OnPropertyChanged(nameof(SecondPlacePlayerName)); }
        }

        private string _thirdPlacePlayerName;
        public string ThirdPlacePlayerName
        {
            get => _thirdPlacePlayerName;
            set { _thirdPlacePlayerName = value; OnPropertyChanged(nameof(ThirdPlacePlayerName)); }
        }

        public ICommand ExitCommand { get; }

        public EndOfMatchViewModel()
        {
            ExitCommand = new RelayCommand(OnExit, CanExit);
        }

        private void OnExit(object parameter)
        {
        }

        private bool CanExit(object parameter)
        {
            return true;
        }

    }
}