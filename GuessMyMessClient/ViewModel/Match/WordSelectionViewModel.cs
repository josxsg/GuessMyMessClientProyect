using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GuessMyMessClient.GameService;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.View.Match;
using GuessMyMessClient.ViewModel.Session;
using GuessMyMessClient.ViewModel.Support;

using ServiceGameFault = GuessMyMessClient.GameService.ServiceFaultDto;

namespace GuessMyMessClient.ViewModel.Match
{
    internal class WordSelectionViewModel : ViewModelBase
    {
        private DispatcherTimer _countdownTimer;
        private bool _wordHasBeenSelected;
        private string _selectedWordToNavigate;

        private string _word1;
        public string Word1
        {
            get
            {
                return _word1;
            }
            set
            {
                _word1 = value; 
                OnPropertyChanged();
            }
        }

        private string _word2;
        public string Word2
        {
            get
            {
                return _word2;
            }
            set
            {
                _word2 = value; 
                OnPropertyChanged();
            }
        }

        private string _word3;
        public string Word3
        {
            get
            {
                return _word3;
            }
            set
            {
                _word3 = value; 
                OnPropertyChanged();
            }
        }

        private int _countdownTime;
        public int CountdownTime
        {
            get
            {
                return _countdownTime;
            }
            set
            {
                _countdownTime = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectWordCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public WordSelectionViewModel()
        {
            _wordHasBeenSelected = false;
            CountdownTime = 10;

            SelectWordCommand = new RelayCommand(SelectWord, CanSelectWord);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            GameClientManager.Instance.ConnectionLost += HandleConnectionLost;
            GameClientManager.Instance.GameEnd += OnGameEnd_Handler;

            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += OnTimerTick;

            Task.Run(() => LoadWordsAsync());
        }

        private void OnGameEnd_Handler(object sender, GameEndEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cleanup();
                ServiceLocator.Navigation.NavigateToEndOfMatch(e.FinalScores);
            });
        }

        private bool CanSelectWord(object parameter)
        {
            return !_wordHasBeenSelected &&
                   !string.IsNullOrEmpty(parameter as string) &&
                   !(parameter as string).Contains("Loading");
        }

        private async Task LoadWordsAsync()
        {
            try
            {
                WordDto[] words = await GameClientManager.Instance.GetRandomWordsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (words != null && words.Length >= 3)
                    {
                        Word1 = GetTranslatedWord(words[0].WordKey);
                        Word2 = GetTranslatedWord(words[1].WordKey);
                        Word3 = GetTranslatedWord(words[2].WordKey);

                        _countdownTimer.Start();
                    }
                    else
                    {
                        MessageBox.Show(
                            Lang.alertWordLoadError,
                            Lang.alertErrorTitle, 
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                });
            }
            catch (FaultException<ServiceGameFault> fex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        fex.Detail.Message,
                        Lang.alertErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning));
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(
                    Lang.alertWordLoadError,
                    Lang.alertConnectionErrorTitle, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error));
            }
        }

        private string GetTranslatedWord(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            string translated = Lang.ResourceManager.GetString(key);

            return string.IsNullOrEmpty(translated) ? key : translated;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            CountdownTime--;

            if (CountdownTime <= 0)
            {
                _countdownTimer.Stop();

                // AHORA SÍ BLOQUEAMOS LA LÓGICA
                _wordHasBeenSelected = true;

                // 1. Verificar si el usuario seleccionó algo manualmente
                if (!string.IsNullOrEmpty(_selectedWordToNavigate))
                {
                    Cleanup();
                    ServiceLocator.Navigation.NavigateToDrawingScreen(_selectedWordToNavigate);
                }
                // 2. Si no seleccionó nada, autoseleccionar la palabra 1
                else
                {
                    if (!string.IsNullOrEmpty(Word1))
                    {
                        // Intentamos enviar al servidor la selección automática
                        try
                        {
                            GameClientManager.Instance.SelectWord(Word1);
                        }
                        catch { } // Ignorar error en autoselección final

                        Cleanup();
                        ServiceLocator.Navigation.NavigateToDrawingScreen(Word1);
                    }
                    else
                    {
                        // Caso extremo: no cargaron palabras y se acabó el tiempo
                        MessageBox.Show(Lang.alertWordAutoSelectFailed, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        HandleConnectionLost();
                    }
                }
            }
        }
        private void SelectWord(object parameter)
        {
            if (CountdownTime > 0)
            {
                HandleWordSelection(parameter as string);
            }
        }

        private void HandleWordSelection(string selectedWord)
        {
            if (string.IsNullOrEmpty(selectedWord))
            {
                return;
            }

            // Actualizamos la variable local. Visualmente el RadioButton ya se puso amarillo por el XAML.
            _selectedWordToNavigate = selectedWord;

            try
            {
                // OPCIONAL: ¿Quieres avisar al servidor cada vez que cambia de opinión?
                // Si sí, deja esta línea. Si prefieres enviar solo al final, muévela a OnTimerTick.
                // Generalmente está bien enviarla aquí para asegurar que el servidor tenga "algo" por si se desconecta.
                GameClientManager.Instance.SelectWord(selectedWord);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertWordSelectError, 
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _wordHasBeenSelected = false;
            }
        }

        private void HandleConnectionLost()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Cleanup();
                MessageBox.Show(
                    Lang.alertConnectionErrorMessage, 
                    Lang.alertConnectionErrorTitle, 
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Application.Current.Shutdown();
            });
        }

        public void Cleanup()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnTimerTick;
            }
            GameClientManager.Instance.ConnectionLost -= HandleConnectionLost;
            GameClientManager.Instance.GameEnd -= OnGameEnd_Handler;
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

        private static void ExecuteMaximizeWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
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
