using GuessMyMessClient.ViewModel.Support;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using GuessMyMessClient.ViewModel.Session;
using System.Linq;
using GuessMyMessClient.View.Match;

namespace GuessMyMessClient.ViewModel.Match
{
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
        private string _wordToDraw;
        public string WordToDraw
        {
            get { return _wordToDraw; }
            set { _wordToDraw = value; OnPropertyChanged(); }
        }

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
                if (InkAttributes != null)
                {
                    InkAttributes.Width = value;
                    InkAttributes.Height = value;
                }
            }
        }

        private InkCanvasEditingMode _editingMode;
        public InkCanvasEditingMode EditingMode
        {
            get { return _editingMode; }
            set { _editingMode = value; OnPropertyChanged(); }
        }

        private Color _currentColor;
        private DrawingTool _currentTool;
        public DrawingTool CurrentTool
        {
            get { return _currentTool; }
            set { _currentTool = value; OnPropertyChanged(); }
        }

        private Point _startPoint;
        private Stroke _currentShapeStroke;

        private DispatcherTimer _countdownTimer;
        private int _remainingTime;
        public int RemainingTime
        {
            get { return _remainingTime; }
            set { _remainingTime = value; OnPropertyChanged(); }
        }

        public ICommand SelectColorCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public ICommand StartShapeCommand { get; }
        public ICommand UpdateShapeCommand { get; }
        public ICommand EndShapeCommand { get; }

        public DrawingScreenViewModel()
        {
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            SelectColorCommand = new RelayCommand(ExecuteSelectColor);
            SelectToolCommand = new RelayCommand(ExecuteSelectTool);

            StartShapeCommand = new RelayCommand(param => StartShape((Point)param));
            UpdateShapeCommand = new RelayCommand(param => UpdateShape((Point)param));
            EndShapeCommand = new RelayCommand(param => EndShape());

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

        private void InitializeTimer()
        {
            RemainingTime = 30;
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
            Console.WriteLine("Time is up. Waiting for other players...");
        }

        private void SaveDrawing()
        {
            try
            {
                Console.WriteLine("Processing drawing before sending...");
                byte[] drawingBytes = ConvertStrokesToByteArray();
                GameClientManager.Instance.SubmitDrawing(drawingBytes);
                Console.WriteLine("Drawing successfully sent to the server!");
                EditingMode = InkCanvasEditingMode.None;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while sending your drawing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSelectColor(object parameter)
        {
            string colorString = parameter as string;
            if (colorString != null)
            {
                _currentColor = (Color)ColorConverter.ConvertFromString(colorString);
                InkAttributes.Color = _currentColor;

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
                    EditingMode = InkCanvasEditingMode.None;
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

        private void StartShape(Point position)
        {
            if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser) return;

            _startPoint = position;
            StylusPointCollection points = new StylusPointCollection(new Point[] { position });
            _currentShapeStroke = new Stroke(points)
            {
                DrawingAttributes = InkAttributes.Clone()
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

        private byte[] ConvertStrokesToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (Strokes != null && Strokes.Count > 0)
                {
                    Strokes.Save(ms);
                    return ms.ToArray();
                }
                return new byte[0];
            }
        }

        private void SubscribeToGameEvents()
        {
            GameClientManager.Instance.GuessingPhaseStart += OnGuessingPhaseStart_FromServer;
            GameClientManager.Instance.ConnectionLost += OnConnectionLost;
        }

        private void OnGuessingPhaseStart_FromServer(object sender, GuessingPhaseStartEventArgs e)
        {
            var drawing = e.Drawing;
            string myUsername = GameClientManager.Instance.GetCurrentUsername();

            if (drawing.OwnerUsername == myUsername)
            {
                ServiceLocator.Navigation.NavigateToWaitingForGuesses(drawing.WordKey);
            }
            else
            {
                ServiceLocator.Navigation.NavigateToGuess(drawing);
            }
        }

        private void OnConnectionLost()
        {
            Application.Current.Dispatcher.Invoke(CloseCurrentWindow);
        }

        private void UnsubscribeFromGameEvents()
        {
            GameClientManager.Instance.GuessingPhaseStart -= OnGuessingPhaseStart_FromServer;
            GameClientManager.Instance.ConnectionLost -= OnConnectionLost;
        }

        private void CloseCurrentWindow()
        {
            UnsubscribeFromGameEvents();
            _countdownTimer?.Stop();

            Window currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            currentWindow?.Close();
        }

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
