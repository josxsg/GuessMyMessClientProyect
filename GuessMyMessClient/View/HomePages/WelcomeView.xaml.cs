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
using GuessMyMessClient.ViewModel.HomePages;

namespace GuessMyMessClient.View.HomePages
{
    /// <summary>
    /// Lógica de interacción para WelcomeView.xaml
    /// </summary>
    public partial class WelcomeView : Window
    {
        public WelcomeView()
        {
            InitializeComponent();
            this.DataContext = new WelcomeViewModel();
        }
    }
}
