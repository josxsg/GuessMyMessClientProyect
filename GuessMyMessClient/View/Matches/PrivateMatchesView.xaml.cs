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
using GuessMyMessClient.ViewModel.Matches;

namespace GuessMyMessClient.View.Matches
{
    /// <summary>
    /// Lógica de interacción para PrivateMatches.xaml
    /// </summary>
    public partial class PrivateMatchesView : Window
    {
        public PrivateMatchesView()
        {
            InitializeComponent();
            this.DataContext = new PrivateMatchesViewModel();
        }
    }
}
