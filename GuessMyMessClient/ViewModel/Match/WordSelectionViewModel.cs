using System;
using System.Windows.Input;
using System.Windows;
using GuessMyMessClient.ViewModel.Support;
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Session; // Para GameClientManager
using GuessMyMessClient.GameService;     // Para WordDto
using System.Linq; // Necesario para .OfType<>()
using System.Windows.Threading; // Para el DispatcherTimer

namespace GuessMyMessClient.ViewModel.Match
{
    internal class WordSelectionViewModel : ViewModelBase
    {
        private DispatcherTimer _countdownTimer;
        private bool _wordHasBeenSelected;

        private string _word1;
        public string Word1
        {
            get { return _word1; }
            set { _word1 = value; OnPropertyChanged(); }
        }

        private string _word2;
        public string Word2
        {
            get { return _word2; }
            set { _word2 = value; OnPropertyChanged(); }
        }

        private string _word3;
        public string Word3
        {
            get { return _word3; }
            set { _word3 = value; OnPropertyChanged(); }
        }

        // --- Propiedad para el Contador ---
        private int _countdownTime;
        public int CountdownTime
        {
            get { return _countdownTime; }
            set { _countdownTime = value; OnPropertyChanged(); }
        }

        public ICommand SelectWordCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public WordSelectionViewModel()
        {
            _wordHasBeenSelected = false;
            CountdownTime = 10; // Inicia en 10

            // 1. Inicializar comandos
            SelectWordCommand = new RelayCommand(SelectWord, CanSelectWord);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            // 2. Suscribirse a eventos del manager
            GameClientManager.Instance.ConnectionLost += HandleConnectionLost;

            // 3. Configurar el temporizador
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += OnTimerTick;

            // 4. Cargar las palabras (el timer se inicia en este método)
            LoadWords();
        }

        private bool CanSelectWord(object parameter)
        {
            // Evita clics antes de que carguen las palabras
            return !_wordHasBeenSelected && !string.IsNullOrEmpty(parameter as string) && !(parameter as string).Contains("Cargando");
        }

        private async void LoadWords()
        {
            try
            {
                WordDto[] words = await GameClientManager.Instance.GetRandomWordsAsync();

                if (words != null && words.Length >= 3)
                {
                    Word1 = words[0].WordKey;
                    Word2 = words[1].WordKey;
                    Word3 = words[2].WordKey;

                    // ¡Iniciamos el contador AHORA!
                    _countdownTimer.Start();
                }
                else
                {
                    MessageBox.Show("No se pudieron cargar las palabras del servidor.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar palabras: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento que se dispara cada segundo
        private void OnTimerTick(object sender, EventArgs e)
        {
            CountdownTime--;

            if (CountdownTime <= 0)
            {
                _countdownTimer.Stop();

                // ¡Tiempo fuera! Seleccionamos Word1 automáticamente
                // Verificamos que las palabras no sean nulas antes de auto-seleccionar
                if (!string.IsNullOrEmpty(Word1))
                {
                    HandleWordSelection(Word1);
                }
                else
                {
                    // Fallback en caso de que LoadWords falle y el timer siga
                    MessageBox.Show("Error de carga, no se pudo auto-seleccionar palabra.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    HandleConnectionLost(); // Salir de la pantalla
                }
            }
        }

        // Comando para la selección MANUAL
        private void SelectWord(object parameter)
        {
            _countdownTimer.Stop(); // Detenemos el timer
            HandleWordSelection(parameter as string);
        }

        // Lógica de selección y navegación (Manual o Automática)
        // --- En WordSelectionViewModel.cs ---

        // --- En WordSelectionViewModel.cs ---

        private void HandleWordSelection(string selectedWord)
        {
            // 1. Evitar clics duplicados (esto ya lo tenías, ¡está bien!)
            if (_wordHasBeenSelected) return;
            _wordHasBeenSelected = true;

            // 2. Detener el contador
            _countdownTimer?.Stop();

            if (string.IsNullOrEmpty(selectedWord)) return;

            try
            {
                // 3. Enviar la palabra al servidor
                GameClientManager.Instance.SelectWord(selectedWord);

                // 4. Buscar la ventana actual para cerrarla
                Window currentWindow = Application.Current.Windows
                    .OfType<WordSelectionView>() // <-- Necesita saber su propia Vista
                    .FirstOrDefault();

                if (currentWindow != null)
                {
                    // 5. --- ¡ESTA ES LA CORRECCIÓN! ---
                    // Le decimos al servicio de navegación que abra la siguiente pantalla
                    ServiceLocator.Navigation.NavigateToDrawingScreen(selectedWord);

                    // 6. Esta ventana (WordSelection) se cierra a sí misma
                    // porque la navegación la inicia ella.
                    currentWindow.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar la palabra: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        // --- Manejo de Conexión y Ventana ---

        private void HandleConnectionLost()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Cleanup();
                MessageBox.Show("Se perdió la conexión con el servidor del juego.", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);

                // TODO: Navegar al Lobby/Login
                Application.Current.Shutdown();
            });
        }

        private void Cleanup()
        {
            // Detenemos el timer y quitamos el evento al limpiar
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnTimerTick;
            }
            GameClientManager.Instance.ConnectionLost -= HandleConnectionLost;
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
                Cleanup();
                GameClientManager.Instance.Disconnect();
                Application.Current.Shutdown();
            }
        }

        private void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void ExecuteMinimizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}