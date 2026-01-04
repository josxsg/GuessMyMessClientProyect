using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Support;
using System.Windows;
using GuessMyMessClient.AuthService;

namespace GuessMyMessClient.ViewModel
{
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

        protected void ShowAlert(string message, string title, MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        protected void ShowServiceError(ServiceErrorType errorType)
        {
            var (title, message) = ErrorManager.GetErrorMessage(errorType);
            ShowAlert(message, title, MessageBoxImage.Warning);
        }

        protected void HandleException(Exception ex)
        {
            if (ex is TimeoutException || ex is EndpointNotFoundException || ex is CommunicationException)
            {
                ShowAlert(Lang.alertConnectionErrorMessage, Lang.alertConnectionErrorTitle, MessageBoxImage.Error);
            }
            else if (ex is FaultException<ServiceFaultDto> faultEx)
            {
                ShowServiceError(faultEx.Detail.ErrorType);
            }
            else
            {
                Console.WriteLine(ex.ToString());
                ShowAlert(Lang.alertUnknownErrorMessage, Lang.alertErrorTitle, MessageBoxImage.Error);
            }
        }
    }
}
