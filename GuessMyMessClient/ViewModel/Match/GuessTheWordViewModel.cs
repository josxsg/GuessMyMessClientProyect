using System;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using GuessMyMessClient.GameService;
using GuessMyMessClient.ViewModel.Session;
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
            set
            {
                _userGuess = value;
                OnPropertyChanged();
            }
        }

        private bool _guessSent;
        private readonly int _drawingId;

        public ICommand ConfirmGuessCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public GuessTheWordViewModel()
        {
            _guessSent = false;
            ConfirmGuessCommand = new RelayCommand(ExecuteConfirmGuess, CanExecuteConfirmGuess);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            DrawingToGuess = new StrokeCollection();
            UserGuess = string.Empty;
        }

        public GuessTheWordViewModel(DrawingDto drawing) : this()
        {
            if (drawing == null)
            {
                MessageBox.Show("Error: No drawing received to guess.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _drawingId = drawing.DrawingId;
            LoadDrawingFromBytes(drawing.DrawingData);

            GameClientManager.Instance.ShowNextDrawing += OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart += OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost_Handler;
        }

        private void LoadDrawingFromBytes(byte[] drawingData)
        {
            if (drawingData == null || drawingData.Length == 0)
            {
                DrawingToGuess = new StrokeCollection();
                return;
            }

            try
            {
                using (var ms = new MemoryStream(drawingData))
                {
                    DrawingToGuess = new StrokeCollection(ms);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error loading drawing: {ex.Message}");
                DrawingToGuess = new StrokeCollection();
            }
        }

        private bool CanExecuteConfirmGuess(object parameter)
        {
            return !_guessSent && !string.IsNullOrWhiteSpace(UserGuess);
        }

        private void ExecuteConfirmGuess(object parameter)
        {
            _guessSent = true;

            try
            {
                GameClientManager.Instance.SubmitGuess(UserGuess, _drawingId);
                UserGuess = "Answer sent! Waiting for other players...";
                OnPropertyChanged(nameof(UserGuess));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending answer: {ex.Message}");
                _guessSent = false;
            }
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

        private void Cleanup()
        {
            GameClientManager.Instance.ShowNextDrawing -= OnShowNextDrawing_Handler;
            GameClientManager.Instance.AnswersPhaseStart -= OnAnswersPhaseStart_Handler;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost_Handler;
        }

        private void ExecuteCloseWindow(object parameter)
        {
            Cleanup();
            GameClientManager.Instance.Disconnect();
            ServiceLocator.Navigation.CloseCurrentGameWindow();
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
