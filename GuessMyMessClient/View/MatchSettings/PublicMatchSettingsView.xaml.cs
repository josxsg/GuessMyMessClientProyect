using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GuessMyMessClient.ViewModel.MatchSettings;

namespace GuessMyMessClient.View.MatchSettings
{
    /// <summary>
    /// Lógica de interacción para PublicMatchSettings.xaml
    /// </summary>
    public partial class PublicMatchSettingsView : Window
    {
        public PublicMatchSettingsView()
        {
            InitializeComponent();
            this.DataContext = new PublicMatchSettingsViewModel();
        }
    }
}
