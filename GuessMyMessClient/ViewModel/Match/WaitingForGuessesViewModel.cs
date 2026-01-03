using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;
using System;
using System.Linq;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Match
{
    public class WaitingForGuessesViewModel : ViewModelBase
    {
        private string _word;
        public string Word
        {
            get
            {
                return _word;
            }
            set
            {
                _word = value; 
                OnPropertyChanged();
            }
        }

        public WaitingForGuessesViewModel(string word)
        {
            Word = word;

            GameClientManager.Instance.ShowNextDrawing += OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart += OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost_Handler;
            GameClientManager.Instance.GameEnd += OnGameEnd_Handler;
        }

        private static void OnShowNextDrawing_Handler(object sender, ShowNextDrawingEventArgs e)
        {
            string myUsername = GameClientManager.Instance.GetCurrentUsername();

            if (e.NextDrawing.OwnerUsername == myUsername)
            {
                ServiceLocator.Navigation.NavigateToWaitingForGuesses(e.NextDrawing.WordKey);
            }
            else
            {
                ServiceLocator.Navigation.NavigateToNextGuess(e.NextDrawing);
            }
        }

        private void OnAnswersPhaseStart_Handler(object sender, AnswersPhaseStartEventArgs e)
        {
            Cleanup();
            ServiceLocator.Navigation.NavigateToAnswers(e.AllDrawings, e.AllGuesses, e.AllScores);
        }

        private void OnConnectionLost_Handler()
        {
            Cleanup();
            ServiceLocator.Navigation.CloseCurrentGameWindow();
        }

        private void OnGameEnd_Handler(object sender, GameEndEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cleanup();
                ServiceLocator.Navigation.NavigateToEndOfMatch(e.FinalScores);
            });
        }

        public void Cleanup()
        {
            GameClientManager.Instance.ShowNextDrawing -= OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart -= OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost_Handler;
            GameClientManager.Instance.GameEnd -= OnGameEnd_Handler;
        }
    }
}
