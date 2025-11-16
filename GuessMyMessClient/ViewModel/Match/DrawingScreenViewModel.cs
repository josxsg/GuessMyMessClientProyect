using GuessMyMessClient.ViewModel.Support;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO; // Para MemoryStream (convertir dibujo a bytes)
using GuessMyMessClient.ViewModel.Session; // Para acceder a GameClientManager
using System.Linq; // Para buscar la ventana
using GuessMyMessClient.View.Match; // Para poder crear GuessTheWordView

namespace GuessMyMessClient.ViewModel.Match
{
    // Enum para identificar la herramienta activa
    public enum DrawingTool
    {
        Pencil,
        Eraser,
        Triangle,
        Circle,
        Square
    }

    internal class DrawingScreenViewModel : ViewModelBase
    {
        // --- Propiedades de Texto e Interfaz ---
        private string _wordToDraw;
        public string WordToDraw
        {
            get { return _wordToDraw; }
            set { _wordToDraw = value; OnPropertyChanged(); }
        }

        // --- Propiedades de Dibujo ---
        private StrokeCollection _strokes;
        public StrokeCollection Strokes
        {
            get { return _strokes; }
            set { _strokes = value; OnPropertyChanged(); }
        }

        private DrawingAttributes _inkAttributes;
        public DrawingAttributes InkAttributes
        {
            get { return _inkAttributes; }
            set { _inkAttributes = value; OnPropertyChanged(); }
        }

        private double _brushThickness;
        public double BrushThickness
        {
            get { return _brushThickness; }
            set
            {
                _brushThickness = value;
                OnPropertyChanged();

                // Actualiza el grosor del Lápiz y las Figuras
                if (InkAttributes != null)
                {
                    InkAttributes.Width = value;
                    InkAttributes.Height = value;
                }
                // Nota: El grosor del borrador se actualiza automáticamente en la Vista 
                // gracias al binding con InkCanvasBinder.EraserSize
            }
        }

        private InkCanvasEditingMode _editingMode;
        public InkCanvasEditingMode EditingMode
        {
            get { return _editingMode; }
            set { _editingMode = value; OnPropertyChanged(); }
        }

        private Color _currentColor;

        // --- Herramienta Actual ---
        private DrawingTool _currentTool;
        public DrawingTool CurrentTool
        {
            get { return _currentTool; }
            set { _currentTool = value; OnPropertyChanged(); }
        }

        // --- Variables para el cálculo de figuras ---
        private Point _startPoint;
        private Stroke _currentShapeStroke;

        // --- Propiedades del Temporizador ---
        private DispatcherTimer _countdownTimer;
        private int _remainingTime;
        public int RemainingTime
        {
            get { return _remainingTime; }
            set { _remainingTime = value; OnPropertyChanged(); }
        }

        // --- Comandos ---
        public ICommand SelectColorCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        // Comandos para eventos del Mouse (Usados por InkCanvasBinder)
        public ICommand StartShapeCommand { get; }
        public ICommand UpdateShapeCommand { get; }
        public ICommand EndShapeCommand { get; }

        // --- Constructor ---
        public DrawingScreenViewModel()
        {
            // Comandos de la interfaz
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            SelectColorCommand = new RelayCommand(ExecuteSelectColor);
            SelectToolCommand = new RelayCommand(ExecuteSelectTool);

            // Comandos de dibujo de figuras (reciben un Point desde la Vista)
            StartShapeCommand = new RelayCommand(param => StartShape((Point)param));
            UpdateShapeCommand = new RelayCommand(param => UpdateShape((Point)param));
            EndShapeCommand = new RelayCommand(param => EndShape());

            // Inicialización
            Strokes = new StrokeCollection();
            InkAttributes = new DrawingAttributes();
            _currentColor = Colors.Black;
            InkAttributes.Color = _currentColor;
            InkAttributes.StylusTip = StylusTip.Ellipse;

            CurrentTool = DrawingTool.Pencil;
            EditingMode = InkCanvasEditingMode.Ink;
            BrushThickness = 5;

            InitializeTimer();
            SubscribeToGameEvents();
        }

        public DrawingScreenViewModel(string word) : this()
        {
            WordToDraw = word;
        }

        // --- Lógica del Temporizador ---
        private void InitializeTimer()
        {
            RemainingTime = 30; // 30 segundos
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            RemainingTime--;
            if (RemainingTime <= 0)
            {
                _countdownTimer.Stop();
                OnTimerFinished();
            }
        }

        private void OnTimerFinished()
        {
            EditingMode = InkCanvasEditingMode.None;
            SaveDrawing();
            Console.WriteLine("Tiempo terminado. Esperando a otros jugadores...");
        }

        private void SaveDrawing()
        {
            try
            {
                Console.WriteLine("Procesando dibujo para enviar...");

                // 1. Convertir el Canvas a Bytes
                byte[] drawingBytes = ConvertStrokesToByteArray();

                // 2. Usar el GameClientManager para enviar
                // No hace falta pasar ID ni Usuario, el Manager ya los tiene en memoria
                GameClientManager.Instance.SubmitDrawing(drawingBytes);

                Console.WriteLine("¡Dibujo enviado exitosamente al servidor!");

                // Opcional: Aquí podrías mostrar un mensaje de "Esperando a otros jugadores..."
                // o deshabilitar la edición para que no siga dibujando.
                EditingMode = InkCanvasEditingMode.None;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al enviar tu dibujo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Lógica de Selección ---
        private void ExecuteSelectColor(object parameter)
        {
            string colorString = parameter as string;
            if (colorString != null)
            {
                _currentColor = (Color)ColorConverter.ConvertFromString(colorString);
                InkAttributes.Color = _currentColor;

                // Si estaba borrando, vuelve al lápiz al seleccionar color
                if (CurrentTool == DrawingTool.Eraser)
                {
                    ExecuteSelectTool("Pencil");
                }
            }
        }

        private void ExecuteSelectTool(object parameter)
        {
            string tool = parameter as string;
            switch (tool)
            {
                case "Pencil":
                    CurrentTool = DrawingTool.Pencil;
                    EditingMode = InkCanvasEditingMode.Ink;
                    InkAttributes.Color = _currentColor;
                    break;

                case "Eraser":
                    CurrentTool = DrawingTool.Eraser;
                    EditingMode = InkCanvasEditingMode.EraseByPoint;
                    break;

                case "Triangle":
                    CurrentTool = DrawingTool.Triangle;
                    EditingMode = InkCanvasEditingMode.None; // Desactivamos dibujo libre
                    break;

                case "Circle":
                    CurrentTool = DrawingTool.Circle;
                    EditingMode = InkCanvasEditingMode.None;
                    break;

                case "Square":
                    CurrentTool = DrawingTool.Square;
                    EditingMode = InkCanvasEditingMode.None;
                    break;
            }
        }

        // --- Lógica Matemática de Figuras (Zero Code-Behind) ---

        private void StartShape(Point position)
        {
            // Solo actuamos si estamos en modo figuras
            if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser) return;

            _startPoint = position;

            // Creamos el trazo inicial
            StylusPointCollection points = new StylusPointCollection(new Point[] { position });
            _currentShapeStroke = new Stroke(points)
            {
                DrawingAttributes = InkAttributes.Clone() // Usa color y grosor actuales
            };

            Strokes.Add(_currentShapeStroke);
        }

        private void UpdateShape(Point currentPosition)
        {
            if (_currentShapeStroke == null) return;

            StylusPointCollection newPoints = null;

            switch (CurrentTool)
            {
                case DrawingTool.Square:
                    newPoints = CalculateSquarePoints(_startPoint, currentPosition);
                    break;
                case DrawingTool.Triangle:
                    newPoints = CalculateTrianglePoints(_startPoint, currentPosition);
                    break;
                case DrawingTool.Circle:
                    newPoints = CalculateCirclePoints(_startPoint, currentPosition);
                    break;
            }

            if (newPoints != null)
            {
                _currentShapeStroke.StylusPoints = newPoints;
            }
        }

        private void EndShape()
        {
            _currentShapeStroke = null;
        }

        // --- Cálculos Geométricos ---

        private StylusPointCollection CalculateSquarePoints(Point start, Point end)
        {
            var points = new List<Point>
            {
                start,
                new Point(end.X, start.Y),
                end,
                new Point(start.X, end.Y),
                start
            };
            return new StylusPointCollection(points);
        }

        private StylusPointCollection CalculateTrianglePoints(Point start, Point end)
        {
            double topX = (start.X + end.X) / 2;
            var points = new List<Point>
            {
                new Point(topX, start.Y),
                end,
                new Point(start.X, end.Y),
                new Point(topX, start.Y)
            };
            return new StylusPointCollection(points);
        }

        private StylusPointCollection CalculateCirclePoints(Point start, Point end)
        {
            StylusPointCollection points = new StylusPointCollection();
            double centerX = (start.X + end.X) / 2;
            double centerY = (start.Y + end.Y) / 2;
            double radiusX = Math.Abs(end.X - start.X) / 2;
            double radiusY = Math.Abs(end.Y - start.Y) / 2;

            for (int i = 0; i <= 360; i += 5)
            {
                double angle = i * Math.PI / 180;
                double x = centerX + radiusX * Math.Cos(angle);
                double y = centerY + radiusY * Math.Sin(angle);
                points.Add(new StylusPoint(x, y));
            }
            return points;
        }

        // Método auxiliar para convertir los trazos a un array de bytes
        private byte[] ConvertStrokesToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (Strokes != null && Strokes.Count > 0)
                {
                    // Save guarda los trazos en formato ISF (Ink Serialized Format) nativo de WPF
                    Strokes.Save(ms);
                    return ms.ToArray();
                }
                // Si no dibujó nada, enviamos un array vacío (o podrías impedir guardar)
                return new byte[0];
            }
        }

        private void SubscribeToGameEvents()
        {
            // Escuchamos la señal del servidor para la siguiente fase
            GameClientManager.Instance.GuessingPhaseStart += OnGuessingPhaseStart_FromServer;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost;
        }

        private void OnGuessingPhaseStart_FromServer(object sender, GuessingPhaseStartEventArgs e)
        {
            // Obtenemos el DTO del dibujo
            var drawing = e.Drawing;

            // Comprobamos si el dueño del dibujo soy YO
            // (Necesitas una forma de obtener tu propio username. 
            // Asumiré que lo tienes en 'SessionManager.Instance.Username' o algo similar.
            // Usaré GameClientManager.Instance.GetCurrentUsername() como ejemplo)

            // --- MODIFICACIÓN REQUERIDA EN GameClientManager ---
            // Necesitas una forma de exponer el _currentUsername.
            // Añade esto a GameClientManager.cs:
            // public string GetCurrentUsername() => _currentUsername;

            string myUsername = GameClientManager.Instance.GetCurrentUsername();

            if (drawing.OwnerUsername == myUsername)
            {
                // 1. SOY EL ARTISTA: Voy a la pantalla de espera
                ServiceLocator.Navigation.NavigateToWaitingForGuesses(drawing.WordKey);
            }
            else
            {
                // 2. SOY ADIVINADOR: Voy a la pantalla de adivinar
                ServiceLocator.Navigation.NavigateToGuess(drawing);
            }
        }

        private void OnConnectionLost()
        {
            // Si perdemos conexión, cerramos esta ventana. 
            // GameClientManager se encargará de mostrar el error.
            Application.Current.Dispatcher.Invoke(CloseCurrentWindow);
        }

        private void UnsubscribeFromGameEvents()
        {
            // Limpiamos los eventos para evitar fugas de memoria
            GameClientManager.Instance.GuessingPhaseStart -= OnGuessingPhaseStart_FromServer;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost;
        }

        private void CloseCurrentWindow()
        {
            // 1. Desuscribirnos de todo
            UnsubscribeFromGameEvents();

            // 2. Detener el timer si sigue activo (por si acaso)
            _countdownTimer?.Stop();

            // 3. Buscar y cerrar la ventana actual
            Window currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            currentWindow?.Close();
        }

        // --- Comandos de Ventana ---
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