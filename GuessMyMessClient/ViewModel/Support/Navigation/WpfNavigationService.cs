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
                // Asegurarnos de que se ejecute en el hilo de la UI
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _currentMatchWindow.Close();
                    _currentMatchWindow = null;
                });
            }
        }

        // --- Implementaciones de Navegación ---

        public void NavigateToDrawingScreen(string word)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow(); // Cierra la ventana anterior (WordSelection)

                var vm = new DrawingScreenViewModel(word);
                var view = new DrawingScreenView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view; // Almacena la nueva ventana
            });
        }

        public void NavigateToGuess(DrawingDto drawing)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow(); // Cierra DrawingScreenView

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

                // --- ¡ESTA ES LA CONVERSIÓN CLAVE! ---
                var drawingsList = drawings?.ToList() ?? new List<DrawingDto>();
                var guessesList = guesses?.ToList() ?? new List<GuessDto>();
                var scoresList = scores?.ToList() ?? new List<PlayerScoreDto>();

                // Pasamos las Listas (List<T>) al ViewModel
                var vm = new AnswersScreenViewModel(drawingsList, guessesList, scoresList);
                var view = new AnswersScreenView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }

        public void NavigateToNextGuess(DrawingDto nextDrawing)
        {
            // Esto es idéntico a NavigateToGuess
            NavigateToGuess(nextDrawing);
        }

        public void NavigateToEndOfMatch(PlayerScoreDto[] finalScores)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();

                // --- ¡ESTA ES LA CONVERSIÓN CLAVE! ---
                var scoresList = finalScores?.ToList() ?? new List<PlayerScoreDto>();

                // Pasamos la Lista (List<T>) al ViewModel
                //var vm = new EndOfMatchViewModel(scoresList); // Asumiendo que EndOfMatchViewModel espera List<T>
                //var view = new EndOfMatchView { DataContext = vm };

                //view.Show();
                //_currentMatchWindow = view;
            });
        }

        public void NavigateToWaitingForGuesses(string word)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow(); // Cierra DrawingScreenView o AnswersScreenView

                var vm = new WaitingForGuessesViewModel(word);
                var view = new WaitingForGuessesView { DataContext = vm };

                view.Show();
                _currentMatchWindow = view;
            });
        }
    }
}
