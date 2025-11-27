using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;
using System;
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
        }

        private void OnShowNextDrawing_Handler(object sender, ShowNextDrawingEventArgs e)
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

        public void Cleanup()
        {
            GameClientManager.Instance.ShowNextDrawing -= OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart -= OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost_Handler;
        }
    }
}
