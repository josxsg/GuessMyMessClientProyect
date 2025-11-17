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
        void RegisterCurrentWindow(System.Windows.Window window);

        void NavigateToDrawingScreen(string word);

        void NavigateToGuess(DrawingDto drawing);

        void NavigateToAnswers(DrawingDto[] drawings, GuessDto[] guesses, PlayerScoreDto[] scores);

        void NavigateToNextGuess(DrawingDto nextDrawing);

        void NavigateToEndOfMatch(PlayerScoreDto[] finalScores);

        void NavigateToWaitingForGuesses(string word);

        void CloseCurrentGameWindow();
    }
}