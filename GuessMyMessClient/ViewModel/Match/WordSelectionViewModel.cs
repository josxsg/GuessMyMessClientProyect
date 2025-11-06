using System;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.ViewModel.Support;
using GuessMyMessClient.View.Match;

namespace GuessMyMessClient.ViewModel.Match
{
    internal class WordSelectionViewModel : ViewModelBase
    {
        private string _word1;
        public string Word1
        {
            get { return _word1; }
            set
            {
                _word1 = value;
                OnPropertyChanged();
            }
        }

        private string _word2;
        public string Word2
        {
            get { return _word2; }
            set
            {
                _word2 = value;
                OnPropertyChanged();
            }
        }

        private string _word3;
        public string Word3
        {
            get { return _word3; }
            set
            {
                _word3 = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectWordCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public WordSelectionViewModel()
        {
            SelectWordCommand = new RelayCommand(SelectWord);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            LoadWords();
        }

        private void LoadWords()
        {
            Word1 = "Perro";
            Word2 = "Gato";
            Word3 = "Ratón";
        }

        private void SelectWord(object parameter)
        {
            string selectedWord = parameter as string;

            if (selectedWord != null && parameter is Window currentWindow)
            {
                var drawingView = new DrawingScreenView();
                drawingView.Show();
                currentWindow.Close();
            }
        }

        private static void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                Application.Current.Shutdown();
            }
        }

        private static void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private static void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}
