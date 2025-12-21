using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;             
using System.Globalization;    
using System.Threading;          
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel
{
    public class LanguageItem
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false; 
            }

            field = value; 
            OnPropertyChanged(propertyName); 
            return true; 
        }

        protected void ChangeLanguageAndRestart<TWindow>(string cultureCode) where TWindow : Window, new()
        {
            try
            {
                if (Thread.CurrentThread.CurrentUICulture.Name == cultureCode) return;

                CultureInfo newCulture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
                Lang.Culture = newCulture;

                var newWindow = new TWindow();
                newWindow.Show();

                var oldWindow = Application.Current.Windows.OfType<TWindow>().FirstOrDefault(w => w != newWindow && w.IsLoaded)
                                ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != newWindow);

                if (oldWindow != null)
                {
                    if (Application.Current.MainWindow == oldWindow)
                    {
                        Application.Current.MainWindow = newWindow;
                    }
                    oldWindow.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Lang.alertChangeLanguageError,
                    Lang.alertErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
