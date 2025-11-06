using GuessMyMessClient.ViewModel.Support;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace GuessMyMessClient.ViewModel.Match
{
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

        public ICommand SelectColorCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public DrawingScreenViewModel()
        {
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            SelectColorCommand = new RelayCommand(ExecuteSelectColor);
            SelectToolCommand = new RelayCommand(ExecuteSelectTool);
            Strokes = new StrokeCollection();
            InkAttributes = new DrawingAttributes();
            _currentColor = Colors.Black;
            InkAttributes.Color = _currentColor;
            InkAttributes.StylusTip = StylusTip.Ellipse;
            EditingMode = InkCanvasEditingMode.Ink;
            BrushThickness = 5;
            WordToDraw = "Palabra (Diseño)";
        }

        public DrawingScreenViewModel(string word) : this()
        {
            WordToDraw = word;
        }

        private void ExecuteSelectColor(object parameter)
        {
            string colorString = parameter as string;
            if (colorString != null)
            {
                _currentColor = (Color)ColorConverter.ConvertFromString(colorString);
                InkAttributes.Color = _currentColor;
                EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        private void ExecuteSelectTool(object parameter)
        {
            string tool = parameter as string;
            switch (tool)
            {
                case "Pencil":
                    EditingMode = InkCanvasEditingMode.Ink;
                    InkAttributes.Color = _currentColor;
                    break;

                case "Eraser":
                    EditingMode = InkCanvasEditingMode.EraseByPoint;
                    break;

                case "Circle":
                case "Square":
                case "Triangle":
                    EditingMode = InkCanvasEditingMode.None;
                    MessageBox.Show($"Herramienta de forma ({tool}) aún no implementada.");
                    break;
            }
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
