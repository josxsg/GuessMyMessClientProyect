using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GuessMyMessClient.GameService;
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Support;
using GuessMyMessClient.Properties.Langs;
using ServiceGameFault = GuessMyMessClient.GameService.ServiceFaultDto;
using GuessMyMessClient.ViewModel.Session;

namespace GuessMyMessClient.ViewModel.Match
{
    public class PlayerViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value; 
                OnPropertyChanged(nameof(Username));
            }
        }

        private int _score;
        public int Score
        {
            get
            {
                return _score;
            }
            set
            {
                _score = value; 
                OnPropertyChanged(nameof(Score));
            }
        }
    }

    public class ChatMessageViewModel : ViewModelBase
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class AnswersScreenViewModel : ViewModelBase
    {
        private const int SecondsPerGuess = 3;

        private readonly List<DrawingDto> _allDrawings;
        private readonly List<GuessDto> _allGuesses;
        private List<GuessDto> _guessesForCurrentDrawing;
        private DispatcherTimer _timer;
        private int _drawingIndex = -1;
        private int _guessIndex = -1;

        private string _drawingArtistName;
        public string DrawingArtistName
        {
            get
            {
                return _drawingArtistName;
            }
            set
            {
                _drawingArtistName = value; 
                OnPropertyChanged(nameof(DrawingArtistName));
            }
        }

        private StrokeCollection _currentDrawingStrokes;
        public StrokeCollection CurrentDrawingStrokes
        {
            get
            {
                return _currentDrawingStrokes;
            }
            set
            {
                _currentDrawingStrokes = value; 
                OnPropertyChanged(nameof(CurrentDrawingStrokes));
            }
        }

        private string _currentDisplayedGuess;
        public string CurrentDisplayedGuess
        {
            get
            {
                return _currentDisplayedGuess;
            }
            set
            {
                _currentDisplayedGuess = value; 
                OnPropertyChanged(nameof(CurrentDisplayedGuess));
            }
        }

        private Brush _currentGuessColor;
        public Brush CurrentGuessColor
        {
            get
            {
                return _currentGuessColor;
            }
            set
            {
                _currentGuessColor = value; 
                OnPropertyChanged(nameof(CurrentGuessColor));
            }
        }

        public ObservableCollection<PlayerViewModel> PlayerList { get; set; }
        public ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; }

        private string _newChatMessage;
        public string NewChatMessage
        {
            get
            {
                return _newChatMessage;
            }
            set
            {
                _newChatMessage = value;
                OnPropertyChanged(nameof(NewChatMessage));
            }
        }

        public ICommand SendMessageCommand { get; }

        public AnswersScreenViewModel(List<DrawingDto> allDrawings, List<GuessDto> allGuesses, List<PlayerScoreDto> allScores)
        {
            _allDrawings = allDrawings ?? new List<DrawingDto>();
            _allGuesses = allGuesses ?? new List<GuessDto>();
            _drawingIndex = -1;
            _guessIndex = -1;

            CurrentGuessColor = Brushes.Black;
            PlayerList = new ObservableCollection<PlayerViewModel>();

            if (allScores != null)
            {
                foreach (var score in allScores.OrderByDescending(s => s.Score))
                {
                    PlayerList.Add(new PlayerViewModel { Username = score.Username, Score = score.Score });
                }
            }

            ChatMessages = new ObservableCollection<ChatMessageViewModel>();
            SendMessageCommand = new RelayCommand(OnSendChatMessage, CanSendChatMessage);

            GameClientManager.Instance.InGameMessageReceived += OnInGameMessageReceived_Handler;
            GameClientManager.Instance.GameEnd += CloseOnNextPhase;
            GameClientManager.Instance.ConnectionLost += CloseOnDisconnect;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(SecondsPerGuess) };
            _timer.Tick += OnTimerTick;

            ShowNextItem();
            _timer.Start();
        }

        private StrokeCollection LoadStrokesFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new StrokeCollection();
            }
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return new StrokeCollection(ms);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertUnknownErrorMessage,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return new StrokeCollection();
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            ShowNextItem();
        }

        private void ShowNextItem()
        {
            if (_guessIndex == -1)
            {
                _drawingIndex++;

                if (_drawingIndex >= _allDrawings.Count)
                {
                    _timer.Stop();
                    CurrentDisplayedGuess = Lang.answersScreenLbEndOfAnswers;
                    CurrentGuessColor = Brushes.Black;
                    return;
                }

                var currentDrawing = _allDrawings[_drawingIndex];
                DrawingArtistName = currentDrawing.OwnerUsername;
                CurrentDrawingStrokes = LoadStrokesFromBytes(currentDrawing.DrawingData);

                _guessesForCurrentDrawing = _allGuesses
                    .Where(g => g.DrawingId == currentDrawing.DrawingId)
                    .ToList();

                CurrentDisplayedGuess = $"{Lang.answersScreenMsgTheWordWas} {currentDrawing.WordKey}";
                CurrentGuessColor = Brushes.DarkBlue;

                _guessIndex = 0;
            }
            else
            {
                if (_guessIndex < _guessesForCurrentDrawing.Count)
                {
                    var guess = _guessesForCurrentDrawing[_guessIndex];
                    CurrentDisplayedGuess = $"{guess.GuesserUsername}: {guess.GuessText}";
                    CurrentGuessColor = guess.IsCorrect ? Brushes.Green : Brushes.Red;

                    _guessIndex++;
                }
                else
                {
                    CurrentDisplayedGuess = Lang.answersScreenMsgNextDrawing;
                    CurrentGuessColor = Brushes.Black;

                    _guessIndex = -1;
                }
            }
        }

        private void OnSendChatMessage(object parameter)
        {
            if (!CanSendChatMessage(null))
            {
                return;
            }

            try
            {
                GameClientManager.Instance.SendInGameMessage(NewChatMessage);
                NewChatMessage = string.Empty;
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                MessageBox.Show(
                    fex.Detail.Message,
                    Lang.alertChatError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage,
                    Lang.alertChatError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertErrorSendingMessage,
                    Lang.alertChatError, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private bool CanSendChatMessage(object parameter)
        {
            return !string.IsNullOrEmpty(NewChatMessage);
        }

        private void OnInGameMessageReceived_Handler(object sender, InGameMessageEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageViewModel { Sender = e.Sender, Message = e.Message });
            });
        }

        private void CloseOnNextPhase(object sender, EventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(CloseWindow);
        }

        private void CloseOnDisconnect()
        {
            Application.Current?.Dispatcher.Invoke(CloseWindow);
        }

        private void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
            }

            GameClientManager.Instance.InGameMessageReceived -= OnInGameMessageReceived_Handler;
            GameClientManager.Instance.GameEnd -= CloseOnNextPhase;
            GameClientManager.Instance.ConnectionLost -= CloseOnDisconnect;
        }

        private void CloseWindow()
        {
            Cleanup();
            Window w = Application.Current.Windows
                .OfType<AnswersScreenView>()
                .FirstOrDefault(win => win.DataContext == this);

            w?.Close();
        }
    }
}
