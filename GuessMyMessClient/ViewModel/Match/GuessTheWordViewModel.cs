using System;
using System.IO; // <-- ¡Asegúrate de tener este using!
using System.Windows;
using System.Windows.Ink; // <-- ¡Asegúrate de tener este using!
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

        // Constructor base
        public GuessTheWordViewModel()
        {
            ConfirmGuessCommand = new RelayCommand(ExecuteConfirmGuess);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            DrawingToGuess = new StrokeCollection();
            UserGuess = string.Empty;
        }

        // --- NUEVO CONSTRUCTOR ---
        // Este constructor recibirá los datos del evento OnGuessingPhaseStart
        public GuessTheWordViewModel(byte[] drawingData) : this()
        {
            LoadDrawingFromBytes(drawingData);
        }

        // El constructor obsoleto que recibía StrokeCollection ya no es necesario
        // public GuessTheWordViewModel(StrokeCollection drawing) : this() { ... }

        // --- NUEVO MÉTODO AUXILIAR ---
        private void LoadDrawingFromBytes(byte[] drawingData)
        {
            if (drawingData == null || drawingData.Length == 0)
            {
                DrawingToGuess = new StrokeCollection(); // Dibuja un lienzo vacío
                return;
            }

            try
            {
                // Esta es la conversión "inversa"
                using (var ms = new MemoryStream(drawingData))
                {
                    // El constructor de StrokeCollection puede leer el Stream
                    DrawingToGuess = new StrokeCollection(ms);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al cargar el dibujo: {ex.Message}");
                DrawingToGuess = new StrokeCollection(); // Dibuja lienzo vacío si hay error
            }
        }

        private void ExecuteConfirmGuess(object parameter)
        {
            if (string.IsNullOrWhiteSpace(UserGuess))
            {
                return;
            }

            MessageBox.Show($"Respuesta enviada: {UserGuess}");
            // Aquí llamarías a GameClientManager.Instance.SubmitGuess(UserGuess);
        }

        // ... (El resto de tus métodos ExecuteCloseWindow, etc., no cambian) ...
        private static void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window) Application.Current.Shutdown();
        }

        private static void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private static void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window) window.WindowState = WindowState.Minimized;
        }
    }
}