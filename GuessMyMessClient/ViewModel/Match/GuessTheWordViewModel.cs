using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using GuessMyMessClient.ViewModel.Support;

namespace GuessMyMessClient.ViewModel.Match
{
    internal class GuessTheWordViewModel : ViewModelBase
    {
        private StrokeCollection _drawingToGuess;
        public StrokeCollection DrawingToGuess
        {
            get { return _drawingToGuess; }
            set { _drawingToGuess = value; OnPropertyChanged(); }
        }

        private string _userGuess;
        public string UserGuess
        {
            get { return _userGuess; }
            set { _userGuess = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmGuessCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public GuessTheWordViewModel()
        {
            ConfirmGuessCommand = new RelayCommand(ExecuteConfirmGuess);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            DrawingToGuess = new StrokeCollection();
            UserGuess = string.Empty;
        }

        public GuessTheWordViewModel(StrokeCollection drawing) : this()
        {
            DrawingToGuess = drawing;
        }

        private void ExecuteConfirmGuess(object parameter)
        {
            if (string.IsNullOrWhiteSpace(UserGuess))
            {
                return;
            }

            MessageBox.Show($"Respuesta enviada: {UserGuess}");
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
