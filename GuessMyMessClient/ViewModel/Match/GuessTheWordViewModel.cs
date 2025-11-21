using System;
using System.IO;
using System.ServiceModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using GuessMyMessClient.GameService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;
using ServiceGameFault = GuessMyMessClient.GameService.ServiceFaultDto;

namespace GuessMyMessClient.ViewModel.Match
{
    internal class GuessTheWordViewModel : ViewModelBase
    {
        private StrokeCollection _drawingToGuess;
        public StrokeCollection DrawingToGuess
        {
            get
            {
                return _drawingToGuess;
            }
            set
            {
                _drawingToGuess = value; 
                OnPropertyChanged();
            }
        }

        private string _userGuess;
        public string UserGuess
        {
            get
            {
                return _userGuess;
            }
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
                MessageBox.Show(
                    Lang.alertNoDrawingReceived,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBox.Show(
                    $"{Lang.alertDrawingLoadError}\n{ex.Message}",
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                UserGuess = Lang.statusAnswerSent;
                OnPropertyChanged(nameof(UserGuess));
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                MessageBox.Show(fex.Detail.Message, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                _guessSent = false;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                MessageBox.Show(Lang.alertConnectionErrorMessage, Lang.alertConnectionErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Lang.alertGuessSubmitError}\n{ex.Message}",
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _guessSent = false;
            }
        }

        private void OnShowNextDrawing_Handler(object sender, ShowNextDrawingEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cleanup();
                string myUsername = GameClientManager.Instance.GetCurrentUsername();

                if (e.NextDrawing.OwnerUsername == myUsername)
                {
                    ServiceLocator.Navigation.NavigateToWaitingForGuesses(e.NextDrawing.WordKey);
                }
                else
                {
                    ServiceLocator.Navigation.NavigateToNextGuess(e.NextDrawing);
                }
            });
        }

        private void OnAnswersPhaseStart_Handler(object sender, AnswersPhaseStartEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cleanup();
                ServiceLocator.Navigation.NavigateToAnswers(e.AllDrawings, e.AllGuesses, e.AllScores);
            });
        }

        private void OnConnectionLost_Handler()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(Lang.alertConnectionErrorMessage, Lang.alertConnectionErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                Cleanup();
                ServiceLocator.Navigation.CloseCurrentGameWindow();
            });
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
