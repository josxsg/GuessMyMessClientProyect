using GuessMyMessClient.ViewModel.Support; // Asumo que aquí tienes ViewModelBase y RelayCommand
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // Para InkCanvasEditingMode
using System.Windows.Ink;     // Para StrokeCollection, DrawingAttributes
using System.Windows.Input;
using System.Windows.Media;   // Para Color, ColorConverter

namespace GuessMyMessClient.ViewModel.Match
{
    // 1. Asegúrate de que herede de ViewModelBase
    internal class DrawingScreenViewModel : ViewModelBase
    {
        #region Properties

        // --- Propiedades de la Vista (Bindings) ---

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

                // Actualiza el tamaño del pincel/borrador en tiempo real
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

        // --- Propiedades Internas ---
        private Color _currentColor; // Para guardar el color al usar el borrador

        #endregion

        #region Commands

        // --- Comandos de Herramientas ---
        public ICommand SelectColorCommand { get; }
        public ICommand SelectToolCommand { get; }

        // --- Comandos de Ventana (de tu guía) ---
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor para el diseñador XAML (d:DataContext)
        /// </summary>
        public DrawingScreenViewModel()
        {
            // Inicializar Comandos de Ventana
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            // Inicializar Comandos de Paleta
            SelectColorCommand = new RelayCommand(ExecuteSelectColor);
            SelectToolCommand = new RelayCommand(ExecuteSelectTool);

            // Inicializar Lienzo
            Strokes = new StrokeCollection();
            InkAttributes = new DrawingAttributes();

            // Valores por defecto
            _currentColor = Colors.Black;
            InkAttributes.Color = _currentColor;
            InkAttributes.StylusTip = StylusTip.Ellipse; // Tu idea del "círculo"
            EditingMode = InkCanvasEditingMode.Ink;     // Empezar dibujando
            BrushThickness = 5;                         // Dispara el 'set' y ajusta el tamaño
            WordToDraw = "Palabra (Diseño)";
        }

        /// <summary>
        /// Constructor para la navegación (desde WordSelectionViewModel)
        /// </summary>
        public DrawingScreenViewModel(string word) : this() // Llama al constructor base
        {
            WordToDraw = word;
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Se llama cuando se selecciona un RadioButton del grupo "Color"
        /// </summary>
        private void ExecuteSelectColor(object parameter)
        {
            string colorString = parameter as string;
            if (colorString != null)
            {
                // Convierte el string (ej. "Black") a un objeto Color
                _currentColor = (Color)ColorConverter.ConvertFromString(colorString);

                // Aplica el color al pincel
                InkAttributes.Color = _currentColor;

                // Asegúrate de que estamos en modo "Dibujar" (no borrador ni forma)
                EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        /// <summary>
        /// Se llama cuando se selecciona un RadioButton del grupo "ToolPalette"
        /// </summary>
        private void ExecuteSelectTool(object parameter)
        {
            string tool = parameter as string;
            switch (tool)
            {
                case "Pencil":
                    EditingMode = InkCanvasEditingMode.Ink;
                    InkAttributes.Color = _currentColor; // Restaura el último color usado
                    break;

                case "Eraser":
                    EditingMode = InkCanvasEditingMode.EraseByPoint; // Borra por punto (usa el grosor)
                    // Nota: EraseByStroke borra la línea entera
                    break;

                case "Circle":
                case "Square":
                case "Triangle":
                    EditingMode = InkCanvasEditingMode.None; // Desactiva el dibujo libre
                    // TODO: Aquí va la lógica futura para dibujar formas
                    MessageBox.Show($"Herramienta de forma ({tool}) aún no implementada.");
                    break;
            }
        }

        #endregion

        #region Window Command Methods (Iguales a tu guía)

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

        #endregion
    }
}