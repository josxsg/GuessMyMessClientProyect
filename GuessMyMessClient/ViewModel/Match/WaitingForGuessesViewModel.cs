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
            get => _word;
            set { _word = value; OnPropertyChanged(); }
        }

        public WaitingForGuessesViewModel(string word)
        {
            Word = word;

            // Esta vista se cierra cuando el servidor envía la señal de "Mostrar Respuestas"
            // o si se pierde la conexión.
            GameClientManager.Instance.ShowNextDrawing += OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart += OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost_Handler;
        }

        private void OnShowNextDrawing_Handler(object sender, ShowNextDrawingEventArgs e)
        {
            // Esta lógica se repite, lo cual es correcto
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
            // ¡Se acabaron las adivinanzas! Es hora de ver todas las respuestas.
            Cleanup();
            // Navegamos a la pantalla de respuestas final
            ServiceLocator.Navigation.NavigateToAnswers(e.AllDrawings, e.AllGuesses, e.AllScores);
        }

        private void OnConnectionLost_Handler()
        {
            Cleanup();
            ServiceLocator.Navigation.CloseCurrentGameWindow();
        }

        private void Cleanup()
        {
            GameClientManager.Instance.ShowNextDrawing -= OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart -= OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost_Handler;
        }
    }
}