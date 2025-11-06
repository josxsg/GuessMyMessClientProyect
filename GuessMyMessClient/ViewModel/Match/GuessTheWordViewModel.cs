using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;     // <-- Necesario para StrokeCollection
using System.Windows.Input;  // <-- Necesario para ICommand
using GuessMyMessClient.ViewModel.Support; // <-- Asumo que aquí tienes ViewModelBase y RelayCommand

namespace GuessMyMessClient.ViewModel.Match
{
    // 1. Hereda de ViewModelBase
    internal class GuessTheWordViewModel : ViewModelBase
    {
        #region Properties

        /// <summary>
        /// Almacena el dibujo (StrokeCollection) recibido para mostrar en el InkCanvas.
        /// </summary>
        private StrokeCollection _drawingToGuess;
        public StrokeCollection DrawingToGuess
        {
            get { return _drawingToGuess; }
            set { _drawingToGuess = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Almacena el texto que el usuario escribe en el TextBox.
        /// </summary>
        private string _userGuess;
        public string UserGuess
        {
            get { return _userGuess; }
            set { _userGuess = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Comando para el botón "Confirmar".
        /// </summary>
        public ICommand ConfirmGuessCommand { get; }

        // Comandos de ventana (basados en tu WelcomeViewModel)
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor para el diseñador XAML (d:DataContext)
        /// </summary>
        public GuessTheWordViewModel()
        {
            // Inicializar Comandos
            ConfirmGuessCommand = new RelayCommand(ExecuteConfirmGuess);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            // Inicializar Propiedades
            DrawingToGuess = new StrokeCollection(); // Evita errores en el diseñador
            UserGuess = string.Empty;
        }

        /// <summary>
        /// Constructor usado para la navegación (ej. desde el VM anterior).
        /// </summary>
        /// <param name="drawing">El StrokeCollection del dibujo a adivinar.</param>
        public GuessTheWordViewModel(StrokeCollection drawing) : this() // Llama al constructor base
        {
            DrawingToGuess = drawing;

        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Se ejecuta al presionar el botón "Confirmar".
        /// </summary>
        private void ExecuteConfirmGuess(object parameter)
        {
            if (string.IsNullOrWhiteSpace(UserGuess))
            {
                // Opcional: Mostrar un mensaje si no ha escrito nada
                return;
            }

            MessageBox.Show($"Respuesta enviada: {UserGuess}");

            // TODO: Una vez confirmada, deshabilitar el TextBox y el botón.

            // TODO: Navegar a la siguiente vista (ej. Resultados) cuando el servidor lo indique.
            // if (parameter is Window window)
            // {
            //    window.Close();
            // }
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