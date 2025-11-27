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
        private const int SecondsPerGuessDisplay = 4;
        private const int PointsPerCorrectGuess = 50;
        private const int PointsForArtist = 10;

        private readonly List<DrawingDto> _allDrawings;
        private readonly List<GuessDto> _allGuesses;
        private DispatcherTimer _displayTimer;

        private int _currentDrawingIndex = -1;
        private int _currentGuessIndex = -1;
        private List<GuessDto> _currentDrawingGuesses;

        private string _drawingArtistName;
        public string DrawingArtistName
        {
            get
            {
                return _drawingArtistName;
            }
            set
            {
                SetProperty(ref _drawingArtistName, value);
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
                SetProperty(ref _currentDrawingStrokes, value);
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
                SetProperty(ref _currentDisplayedGuess, value);
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
                SetProperty(ref _currentGuessColor, value);
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
                SetProperty(ref _newChatMessage, value);
            }
        }

        public ICommand SendMessageCommand { get; }

        public AnswersScreenViewModel(List<DrawingDto> allDrawings, List<GuessDto> allGuesses, List<PlayerScoreDto> finalRoundScores)
        {
            _allDrawings = allDrawings ?? new List<DrawingDto>();
            _allGuesses = allGuesses ?? new List<GuessDto>();

            PlayerList = new ObservableCollection<PlayerViewModel>();
            ChatMessages = new ObservableCollection<ChatMessageViewModel>();
            SendMessageCommand = new RelayCommand(OnSendChatMessage, CanSendChatMessage);

            InitializeScoresForAnimation(finalRoundScores);

            GameClientManager.Instance.InGameMessageReceived += OnInGameMessageReceived;
            GameClientManager.Instance.GameEnd += OnGameEnd;
            GameClientManager.Instance.RoundStart += OnRoundStart;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost;

            _displayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(SecondsPerGuessDisplay) };
            _displayTimer.Tick += DisplayTimer_Tick;

            ShowNextItem();
            _displayTimer.Start();
        }

        private void InitializeScoresForAnimation(List<PlayerScoreDto> finalScores)
        {
            var scoresDict = new Dictionary<string, int>();

            foreach (var score in finalScores)
            {
                scoresDict[score.Username] = score.Score;
            }

            foreach (var guess in _allGuesses.Where(g => g.IsCorrect))
            {
                if (scoresDict.ContainsKey(guess.GuesserUsername))
                {
                    scoresDict[guess.GuesserUsername] -= PointsPerCorrectGuess;
                }

                var drawing = _allDrawings.FirstOrDefault(d => d.DrawingId == guess.DrawingId);
                if (drawing != null && scoresDict.ContainsKey(drawing.OwnerUsername))
                {
                    scoresDict[drawing.OwnerUsername] -= PointsForArtist;
                }
            }

            foreach (var kvp in scoresDict.OrderByDescending(x => x.Value))
            {
                PlayerList.Add(new PlayerViewModel { Username = kvp.Key, Score = kvp.Value });
            }
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            ShowNextItem();
        }

        private void ShowNextItem()
        {
            if (_currentGuessIndex == -1)
            {
                _currentDrawingIndex++;

                if (_currentDrawingIndex >= _allDrawings.Count)
                {
                    _displayTimer.Stop();

                    CurrentDisplayedGuess = Lang.answersScreenLbEndOfAnswers;
                    CurrentGuessColor = Brushes.Black;
                    DrawingArtistName = "";
                    CurrentDrawingStrokes = new StrokeCollection();

                    return;
                }

                var drawing = _allDrawings[_currentDrawingIndex];
                DrawingArtistName = drawing.OwnerUsername;
                CurrentDrawingStrokes = LoadStrokesFromBytes(drawing.DrawingData);

                CurrentDisplayedGuess = $"{Lang.answersScreenMsgTheWordWas} {drawing.WordKey}";
                CurrentGuessColor = Brushes.DarkBlue;

                _currentDrawingGuesses = _allGuesses.Where(g => g.DrawingId == drawing.DrawingId).ToList();

                _currentGuessIndex = 0;
            }
            else
            {
                if (_currentGuessIndex < _currentDrawingGuesses.Count)
                {
                    var guess = _currentDrawingGuesses[_currentGuessIndex];
                    CurrentDisplayedGuess = $"{guess.GuesserUsername}: {guess.GuessText}";

                    if (guess.IsCorrect)
                    {
                        CurrentGuessColor = Brushes.Green;
                        AddScoreToPlayer(guess.GuesserUsername, PointsPerCorrectGuess);
                        var currentDrawing = _allDrawings[_currentDrawingIndex];
                        AddScoreToPlayer(currentDrawing.OwnerUsername, PointsForArtist);
                    }
                    else
                    {
                        CurrentGuessColor = Brushes.Red;
                    }

                    _currentGuessIndex++;
                }
                else
                {
                    CurrentDisplayedGuess = "Next drawing...";
                    CurrentGuessColor = Brushes.Gray;
                    _currentGuessIndex = -1;
                }
            }
        }

        private void AddScoreToPlayer(string username, int pointsToAdd)
        {
            var player = PlayerList.FirstOrDefault(p => p.Username == username);
            if (player != null)
            {
                player.Score += pointsToAdd;
            }
        }

        private StrokeCollection LoadStrokesFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return new StrokeCollection();
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return new StrokeCollection(ms);
                }
            }
            catch
            {
                return new StrokeCollection();
            }
        }

        private void OnRoundStart(object sender, RoundStartEventArgs e)
        {
            CleanUp();
            Application.Current.Dispatcher.Invoke(() =>
            {
                ServiceLocator.Navigation.NavigateToWordSelection();
            });
        }

        private void OnGameEnd(object sender, GameEndEventArgs e)
        {
            CleanUp();
            Application.Current.Dispatcher.Invoke(() =>
            {
                ServiceLocator.Navigation.NavigateToEndOfMatch(e.FinalScores);
            });
        }

        private void OnConnectionLost()
        {
            CleanUp();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(Lang.alertConnectionErrorMessage, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                ServiceLocator.Navigation.CloseCurrentGameWindow();
            });
        }

        private void OnSendChatMessage(object obj)
        {
            if (CanSendChatMessage(null))
            {
                GameClientManager.Instance.SendInGameMessage(NewChatMessage);
                NewChatMessage = string.Empty;
            }
        }

        private bool CanSendChatMessage(object obj) => !string.IsNullOrWhiteSpace(NewChatMessage);

        private void OnInGameMessageReceived(object sender, InGameMessageEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageViewModel { Sender = e.Sender, Message = e.Message });
            });
        }

        public void CleanUp()
        {
            if (_displayTimer != null)
            {
                _displayTimer.Stop();
                _displayTimer.Tick -= DisplayTimer_Tick;
                _displayTimer = null;
            }
            GameClientManager.Instance.InGameMessageReceived -= OnInGameMessageReceived;
            GameClientManager.Instance.GameEnd -= OnGameEnd;
            GameClientManager.Instance.RoundStart -= OnRoundStart;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost;
        }
    }
}
