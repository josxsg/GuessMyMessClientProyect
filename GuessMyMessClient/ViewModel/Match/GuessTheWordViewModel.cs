using System;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using GuessMyMessClient.GameService;     // Para DTOs
using GuessMyMessClient.ViewModel.Session; // Para GameClientManager
using GuessMyMessClient.ViewModel.Support; // Para ServiceLocator y ViewModelBase

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
                // No llamamos a RaiseCanExecuteChanged(). 
                // CommandManager.RequerySuggested lo detectará.
            }
        }

        // --- Propiedad para deshabilitar el botón al enviar ---
        private bool _guessSent;

        // --- Campos de la partida ---
        private readonly int _drawingId;

        public ICommand ConfirmGuessCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        // Constructor base
        public GuessTheWordViewModel()
        {
            _guessSent = false;
            // Se enlaza el comando con el método CanExecute
            ConfirmGuessCommand = new RelayCommand(ExecuteConfirmGuess, CanExecuteConfirmGuess);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            DrawingToGuess = new StrokeCollection();
            UserGuess = string.Empty;
        }

        // --- Constructor Principal (Modificado) ---
        public GuessTheWordViewModel(DrawingDto drawing) : this()
        {
            if (drawing == null)
            {
                MessageBox.Show("Error: No se recibió ningún dibujo para adivinar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. Guardamos el ID del dibujo
            _drawingId = drawing.DrawingId;

            // 2. Cargamos los trazos desde los bytes
            LoadDrawingFromBytes(drawing.DrawingData);

            // 3. Nos suscribimos a los eventos del manager
            GameClientManager.Instance.ShowAnswersPhase += OnShowAnswers_Handler;
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
                MessageBox.Show($"Error crítico al cargar el dibujo: {ex.Message}");
                DrawingToGuess = new StrokeCollection();
            }
        }

        // --- Lógica de Envío de Respuesta ---

        private bool CanExecuteConfirmGuess(object parameter)
        {
            // El CommandManager ejecutará esto.
            // Devuelve true si NO se ha enviado Y el texto NO está vacío.
            return !_guessSent && !string.IsNullOrWhiteSpace(UserGuess);
        }

        private void ExecuteConfirmGuess(object parameter)
        {
            // 1. Marcar como enviado
            _guessSent = true;
            // Ocultamos el botón deshabilitándolo (CommandManager lo hará)

            try
            {
                // 2. Enviar la respuesta al servidor
                GameClientManager.Instance.SubmitGuess(UserGuess, _drawingId);

                // 3. Actualizar la UI
                // Cambiamos el texto (esto NO afectará al CanExecute porque _guessSent es false)
                UserGuess = "¡Respuesta enviada! Esperando a los demás...";
                OnPropertyChanged(nameof(UserGuess));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar respuesta: {ex.Message}");
                _guessSent = false; // Habilitar de nuevo si falló
            }
        }

        // --- Lógica de Navegación y Limpieza ---

        private void OnShowAnswers_Handler(object sender, ShowAnswersEventArgs e)
        {
            Cleanup();
            ServiceLocator.Navigation.NavigateToAnswers(e.Drawing, e.Guesses, e.Scores);
        }

        private void OnConnectionLost_Handler()
        {
            Cleanup();
            ServiceLocator.Navigation.CloseCurrentGameWindow();
        }

        private void Cleanup()
        {
            GameClientManager.Instance.ShowAnswersPhase -= OnShowAnswers_Handler;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost_Handler;
        }

        // --- Comandos de Ventana ---
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