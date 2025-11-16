using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GuessMyMessClient.GameService; // Necesario para DTOs
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;

namespace GuessMyMessClient.ViewModel.Match
{
    // Las clases PlayerViewModel y ChatMessageViewModel que tenías están bien,
    // pero añadiré 'Score' a PlayerViewModel para la lista de jugadores.

    public class PlayerViewModel : ViewModelBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        private int _score;
        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(nameof(Score)); }
        }
    }

    public class ChatMessageViewModel : ViewModelBase
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }


    public class AnswersScreenViewModel : ViewModelBase
    {
        private const int SECONDS_PER_GUESS = 3;

        // --- Datos de la Ronda ---
        private readonly List<GuessDto> _allGuesses;
        private DispatcherTimer _timer;
        private int _guessIndex;

        // --- Propiedades de UI ---

        private string _drawingArtistName;
        public string DrawingArtistName
        {
            get => _drawingArtistName;
            set { _drawingArtistName = value; OnPropertyChanged(nameof(DrawingArtistName)); }
        }

        private StrokeCollection _currentDrawingStrokes;
        public StrokeCollection CurrentDrawingStrokes
        {
            get => _currentDrawingStrokes;
            set { _currentDrawingStrokes = value; OnPropertyChanged(nameof(CurrentDrawingStrokes)); }
        }

        private string _currentDisplayedGuess;
        public string CurrentDisplayedGuess
        {
            get => _currentDisplayedGuess;
            set { _currentDisplayedGuess = value; OnPropertyChanged(nameof(CurrentDisplayedGuess)); }
        }

        private Brush _currentGuessColor;
        public Brush CurrentGuessColor
        {
            get => _currentGuessColor;
            set { _currentGuessColor = value; OnPropertyChanged(nameof(CurrentGuessColor)); }
        }

        public ObservableCollection<PlayerViewModel> PlayerList { get; set; }
        public ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; }

        private string _newChatMessage;
        public string NewChatMessage
        {
            get => _newChatMessage;
            set
            {
                _newChatMessage = value;
                OnPropertyChanged(nameof(NewChatMessage));
            }
        }

        public ICommand SendMessageCommand { get; }

        /// <summary>
        /// Constructor principal que recibe los datos de la ronda.
        /// </summary>
        public AnswersScreenViewModel(DrawingDto drawing, List<GuessDto> guesses, List<PlayerScoreDto> scores)
        {
            _allGuesses = guesses ?? new List<GuessDto>();
            _guessIndex = -1;
            CurrentGuessColor = Brushes.Black;

            // 1. Cargar Datos del Dibujo
            DrawingArtistName = drawing.OwnerUsername;
            CurrentDrawingStrokes = LoadStrokesFromBytes(drawing.DrawingData);

            // 2. Cargar Lista de Jugadores y Puntajes
            PlayerList = new ObservableCollection<PlayerViewModel>();
            if (scores != null)
            {
                foreach (var score in scores.OrderByDescending(s => s.Score))
                {
                    PlayerList.Add(new PlayerViewModel { Username = score.Username, Score = score.Score });
                }
            }

            // 3. Configurar Chat
            ChatMessages = new ObservableCollection<ChatMessageViewModel>();
            SendMessageCommand = new RelayCommand(OnSendChatMessage, CanSendChatMessage);
            GameClientManager.Instance.InGameMessageReceived += OnInGameMessageReceived_Handler;

            // 4. Suscribirse a eventos de fin de fase (para aut-cerrarse)
            GameClientManager.Instance.ShowNextDrawing += CloseOnNextPhase;
            GameClientManager.Instance.GameEnd += CloseOnNextPhase;
            GameClientManager.Instance.ConnectionLost += CloseOnDisconnect;

            // 5. Iniciar el temporizador de respuestas
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(SECONDS_PER_GUESS)
            };
            _timer.Tick += OnTimerTick;

            ShowNextGuess(); // Mostrar la palabra correcta primero
            _timer.Start();
        }

        /// <summary>
        /// Constructor de diseño (para el XAML)
        /// </summary>
        public AnswersScreenViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadDesignTimeData();
            }
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar strokes: {ex.Message}");
                return new StrokeCollection();
            }
        }

        // --- Lógica de Adivinanzas ---

        private void OnTimerTick(object sender, EventArgs e)
        {
            ShowNextGuess();
        }

        private void ShowNextGuess()
        {
            _guessIndex++;

            if (_guessIndex == 0)
            {
                // La primera vez, mostramos la palabra correcta
                var drawing = _allGuesses.FirstOrDefault();
                string word = drawing != null ? drawing.WordKey : "???"; // Obtenemos la palabra del primer GuessDto
                CurrentDisplayedGuess = $"La palabra era: {word}";
                CurrentGuessColor = Brushes.DarkBlue;
                return;
            }

            int guessDisplayIndex = _guessIndex - 1; // Ajustamos el índice
            if (guessDisplayIndex < _allGuesses.Count)
            {
                var guess = _allGuesses[guessDisplayIndex];
                CurrentDisplayedGuess = $"{guess.GuesserUsername}: {guess.GuessText}";
                CurrentGuessColor = guess.IsCorrect ? Brushes.Green : Brushes.Red;
            }
            else
            {
                // Terminaron las adivinanzas
                _timer.Stop();
                CurrentDisplayedGuess = "Esperando el siguiente dibujo...";
                CurrentGuessColor = Brushes.Black;
                // No hacemos nada más; el servidor nos moverá a la siguiente fase.
            }
        }


        // --- Lógica de Chat ---

        private void OnSendChatMessage(object parameter)
        {
            try
            {
                GameClientManager.Instance.SendInGameMessage(NewChatMessage);
                NewChatMessage = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar mensaje: {ex.Message}", "Error de Chat", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanSendChatMessage(object parameter)
        {
            return !string.IsNullOrEmpty(NewChatMessage);
        }

        private void OnInGameMessageReceived_Handler(object sender, InGameMessageEventArgs e)
        {
            // Asegurarnos de que se ejecute en el hilo de la UI
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Accedemos a los datos a través del parámetro 'e'
                ChatMessages.Add(new ChatMessageViewModel { Sender = e.Sender, Message = e.Message });
            });
        }


        // --- Lógica de Limpieza y Cierre ---

        private void CloseOnNextPhase(object sender, EventArgs e)
        {
            // El servidor ha enviado una nueva fase, esta ventana debe cerrarse.
            Application.Current?.Dispatcher.Invoke(CloseWindow);
        }

        private void CloseOnDisconnect()
        {
            Application.Current?.Dispatcher.Invoke(CloseWindow);
        }

        private void Cleanup()
        {
            // Detener timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
            }

            // Desuscribirse de eventos
            GameClientManager.Instance.InGameMessageReceived -= OnInGameMessageReceived_Handler;
            GameClientManager.Instance.ShowNextDrawing -= CloseOnNextPhase;
            GameClientManager.Instance.GameEnd -= CloseOnNextPhase;
            GameClientManager.Instance.ConnectionLost -= CloseOnDisconnect;
        }

        private void CloseWindow()
        {
            Cleanup();
            Window w = Application.Current.Windows
                .OfType<AnswersScreenView>() // Busca una ventana de este tipo
                .FirstOrDefault(win => win.DataContext == this); // Cuyo DataContext sea esta instancia

            w?.Close();
        }

        private void LoadDesignTimeData()
        {
            DrawingArtistName = "ArtistaFamoso";
            PlayerList = new ObservableCollection<PlayerViewModel>
            {
                new PlayerViewModel { Username = "Jugador1", Score = 120 },
                new PlayerViewModel { Username = "Jugador2", Score = 90 }
            };
            ChatMessages = new ObservableCollection<ChatMessageViewModel>
            {
                new ChatMessageViewModel { Sender = "Jugador1", Message = "¡Buen dibujo!" },
                new ChatMessageViewModel { Sender = "Jugador2", Message = "Gracias :)" }
            };
            CurrentDisplayedGuess = "Jugador1: Elefante";
            CurrentGuessColor = Brushes.Green;
            // No podemos cargar un StrokeCollection en diseño fácilmente
            CurrentDrawingStrokes = new StrokeCollection();
        }
    }
}