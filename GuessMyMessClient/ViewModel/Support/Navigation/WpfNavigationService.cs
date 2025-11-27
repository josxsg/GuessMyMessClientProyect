using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using GuessMyMessClient.GameService;
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
                var windowToClose = _currentMatchWindow;
                _currentMatchWindow = null;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (windowToClose.DataContext != null)
                    {
                        var vm = windowToClose.DataContext;
                        var method = vm.GetType().GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Instance)
                                  ?? vm.GetType().GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Instance);

                        method?.Invoke(vm, null);
                    }
                    windowToClose.Close();
                });
            }
        }

        public void NavigateToWordSelection()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();
                var vm = new WordSelectionViewModel();
                var view = new WordSelectionView { DataContext = vm };
                view.Show();
                _currentMatchWindow = view;
            });
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
                var vm = new AnswersScreenViewModel(drawings?.ToList(), guesses?.ToList(), scores?.ToList());
                var view = new AnswersScreenView { DataContext = vm };
                view.Show();
                _currentMatchWindow = view;
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

        public void NavigateToNextGuess(DrawingDto nextDrawing)
        {
            NavigateToGuess(nextDrawing);
        }

        public void NavigateToEndOfMatch(PlayerScoreDto[] finalScores)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CloseCurrentGameWindow();
                var vm = new EndOfMatchViewModel(finalScores?.ToList());
                var view = new EndOfMatchView { DataContext = vm };
                view.Show();
                _currentMatchWindow = view;
            });
        }
    }
}
