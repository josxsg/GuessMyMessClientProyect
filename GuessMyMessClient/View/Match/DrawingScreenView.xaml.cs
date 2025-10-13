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
using GuessMyMessClient.ViewModel.Match;

namespace GuessMyMessClient.View.Match
{
    /// <summary>
    /// Lógica de interacción para DrawingScreenView.xaml
    /// </summary>
    public partial class DrawingScreenView : Window
    {
        public DrawingScreenView()
        {
            InitializeComponent();
            this.DataContext = new DrawingScreenViewModel(); 
        }
    }
}
