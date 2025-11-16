using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.GameService;

namespace GuessMyMessClient.ViewModel.Support.Navigation
{
    public interface INavigationService
    {
        // Almacena la ventana actual para cerrarla
        void RegisterCurrentWindow(System.Windows.Window window);

        // Navega de WordSelection a Drawing
        void NavigateToDrawingScreen(string word);

        // Navega de Drawing a Guess
        void NavigateToGuess(DrawingDto drawing);

        // Navega de Guess a Answers
        void NavigateToAnswers(DrawingDto[] drawings, GuessDto[] guesses, PlayerScoreDto[] scores);

        // Navega de Answers de vuelta a Guess
        void NavigateToNextGuess(DrawingDto nextDrawing);

        // Navega de Answers a EndOfMatch
        void NavigateToEndOfMatch(PlayerScoreDto[] finalScores);

        void NavigateToWaitingForGuesses(string word);

        // Cierra la ventana actual (ej. por desconexión)
        void CloseCurrentGameWindow();
    }
}
