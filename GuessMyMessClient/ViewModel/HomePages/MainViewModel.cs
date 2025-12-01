using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GuessMyMessClient.View.HomePages;
using System.Globalization;
using System.Threading;
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.HomePages
{
    public class MainViewModel : ViewModelBase
    {
        public ICommand StartGameCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }

        public List<LanguageItem> Languages { get; }
        private LanguageItem _selectedLanguage;

        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    if (value != null)
                    {
                        ChangeLanguageAndRestart<MainView>(value.Code);
                    }
                }
            }
        }

        public MainViewModel()
        {
            StartGameCommand = new RelayCommand(StartGame);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MaximizeWindowCommand = new RelayCommand(ExecuteMaximizeWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);

            Languages = new List<LanguageItem>
            {
                new LanguageItem { Name = "Español", Code = "es-MX" },
                new LanguageItem { Name = "English", Code = "en-US" }
            };

            string currentCode = Thread.CurrentThread.CurrentUICulture.Name;

            _selectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode)
                                ?? Languages.FirstOrDefault(l => currentCode.StartsWith("es") && l.Code == "es-MX")
                                ?? Languages.FirstOrDefault();
        }


        private void StartGame(object parameter)
        {
            var welcomeView = new WelcomeView();
            welcomeView.Show();

            if (parameter is Window mainWindow)
            {
                mainWindow.Close();
            }
        }

        private void ExecuteCloseWindow(object parameter)
        {
            if (parameter is Window)
            {
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