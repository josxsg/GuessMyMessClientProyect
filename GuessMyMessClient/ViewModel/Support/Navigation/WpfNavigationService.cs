using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.GameService;
using System.Windows;
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Match;

namespace GuessMyMessClient.ViewModel.Support.Navigation
{
    public class WpfNavigationService : INavigationService
    {
        private Window _currentMatchWindow;

        public void RegisterCurrentWindow(Window window)
        {
            _currentMatchWindow = window;
        }

        public void CloseCurrentGameWindow()
        {
            if (_currentMatchWindow != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _currentMatchWindow.Close();
                    _currentMatchWindow = null;
                });
            }
        }

        public void NavigateToDrawingScreen(string word)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                var vm = new DrawingScreenViewModel(word);
                var view = new DrawingScreenView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }

        public void NavigateToGuess(DrawingDto drawing)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                var vm = new GuessTheWordViewModel(drawing);
                var view = new GuessTheWordView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }

        public void NavigateToAnswers(DrawingDto[] drawings, GuessDto[] guesses, PlayerScoreDto[] scores)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                var drawingsList = drawings?.ToList() ?? new List<DrawingDto>();
                var guessesList = guesses?.ToList() ?? new List<GuessDto>();
                var scoresList = scores?.ToList() ?? new List<PlayerScoreDto>();

                var vm = new AnswersScreenViewModel(drawingsList, guessesList, scoresList);
                var view = new AnswersScreenView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }

        public void NavigateToNextGuess(DrawingDto nextDrawing)
        {
            NavigateToGuess(nextDrawing);
        }

        public void NavigateToEndOfMatch(PlayerScoreDto[] finalScores)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                var scoresList = finalScores?.ToList() ?? new List<PlayerScoreDto>();

                //var vm = new EndOfMatchViewModel(scoresList);
                //var view = new EndOfMatchView { DataContext = vm };

                //view.Show();
                //_currentMatchWindow = view;
            });
        }

        public void NavigateToWaitingForGuesses(string word)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                var vm = new WaitingForGuessesViewModel(word);
                var view = new WaitingForGuessesView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }
    }
}
