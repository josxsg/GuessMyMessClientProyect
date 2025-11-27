using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.GameService;
using GuessMyMessClient.ViewModel.Support;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support.Navigation; 

namespace GuessMyMessClient.ViewModel.Match
{
    public class EndOfMatchViewModel : ViewModelBase
    {
        private string _winnerName;
        public string WinnerName
        {
            get
            {
                return _winnerName;
            }
            set
            {
                _winnerName = value; 
                OnPropertyChanged();
            }
        }

        private string _firstPlacePlayerName;
        public string FirstPlacePlayerName
        {
            get
            {
                return _firstPlacePlayerName;
            }
            set
            {
                _firstPlacePlayerName = value; 
                OnPropertyChanged();
            }
        }

        private string _secondPlacePlayerName;
        public string SecondPlacePlayerName
        {
            get
            {
                return _secondPlacePlayerName;
            }
            set
            {
                _secondPlacePlayerName = value;
                OnPropertyChanged();
            }
        }

        private string _thirdPlacePlayerName;
        public string ThirdPlacePlayerName
        {
            get
            {
                return _thirdPlacePlayerName;
            }
            set
            {
                _thirdPlacePlayerName = value;
                OnPropertyChanged();
            }
        }

        public ICommand ExitCommand { get; }

        public EndOfMatchViewModel(List<PlayerScoreDto> finalScores)
        {
            ExitCommand = new RelayCommand(ExecuteExit);
            ProcessScores(finalScores);
        }

        private void ProcessScores(List<PlayerScoreDto> scores)
        {
            if (scores == null || scores.Count == 0)
            {
                return;
            }

            var sorted = scores.OrderByDescending(s => s.Score).ToList();

            if (sorted.Count > 0)
            {
                FirstPlacePlayerName = sorted[0].Username; 
                WinnerName = sorted[0].Username;
            }

            if (sorted.Count > 1)
            {
                SecondPlacePlayerName = sorted[1].Username;
            }

            if (sorted.Count > 2)
            {
                ThirdPlacePlayerName = sorted[2].Username;
            }
        }

        private void ExecuteExit(object parameter)
        {
            GameClientManager.Instance.Disconnect();

            Application.Current.Shutdown();
        }
    }
}