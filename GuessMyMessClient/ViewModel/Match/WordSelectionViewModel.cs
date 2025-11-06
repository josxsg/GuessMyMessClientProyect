using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input; 
using System.Windows;       
using GuessMyMessClient.ViewModel.Support; // Asumo que aquí están ViewModelBase y RelayCommand
using GuessMyMessClient.View.Match; // Para la navegación futura

namespace GuessMyMessClient.ViewModel.Match
{
    // 1. Hereda de ViewModelBase (como tu guía WelcomeViewModel)
    internal class WordSelectionViewModel : ViewModelBase
    {
        #region Properties

        // Propiedades para las 3 palabras.
        // Usan el patrón con OnPropertyChanged() para notificar a la Vista.

        private string _word1;
        public string Word1
        {
            get { return _word1; }
            set
            {
                _word1 = value;
                OnPropertyChanged(); // Notifica a la vista
            }
        }

        private string _word2;
        public string Word2
        {
            get { return _word2; }
            set
            {
                _word2 = value;
                OnPropertyChanged();
            }
        }

        private string _word3;
        public string Word3
        {
            get { return _word3; }
            set
            {
                _word3 = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        // Comando para cuando el jugador selecciona una palabra
        public ICommand SelectWordCommand { get; }

        // Comandos de ventana (copiados de tu WelcomeViewModel)
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        #endregion

        #region Constructor
        public WordSelectionViewModel()
        {
            // Inicializar comandos de jugabilidad
            SelectWordCommand = new RelayCommand(SelectWord);

            // Inicializar comandos de ventana (como en tu guía)
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            // Cargamos las palabras (ej. desde el servidor)
            LoadWords();
        }
        #endregion

        #region Command Methods

        /// <summary>
        /// Carga las palabras a seleccionar.
        /// </summary>
        private void LoadWords()
        {
            // TODO: Aquí iría la lógica para pedir las 3 palabras al servidor.
            // Por ahora, usamos datos de ejemplo:
            Word1 = "Perro";
            Word2 = "Gato";
            Word3 = "Ratón";
        }

        /// <summary>
        /// Se ejecuta cuando un jugador hace clic en un botón de palabra.
        /// </summary>
        private void SelectWord(object parameter)
        {
            string selectedWord = parameter as string;

            if (selectedWord != null && parameter is Window currentWindow)
            {
                // TODO: Aquí va la lógica para enviar la 'selectedWord' al servidor.
                // MessageBox.Show($"Palabra seleccionada: {selectedWord}"); // Para depurar

                // TODO: Navegar a la siguiente vista (ej. DrawingScreenView)
                //      y cerrar esta.
                // var drawingView = new DrawingScreenView();
                // drawingView.Show();
                // currentWindow.Close();
            }
        }

        #endregion

        #region Window Command Methods (Iguales a WelcomeViewModel)

        private static void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                // Siguiendo tu guía, esto cierra la aplicación entera.
                // Si solo debe cerrar la ventana, usa (parameter as Window).Close();
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