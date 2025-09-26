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
using GuessMyMessClient.ViewModel.WaitingRoom;

namespace GuessMyMessClient.View.WaitingRoom
{
    /// <summary>
    /// Lógica de interacción para WaitingRoomPublicMatchView.xaml
    /// </summary>
    public partial class WaitingRoomPublicMatchView : Window
    {
        public WaitingRoomPublicMatchView()
        {
            InitializeComponent();
            this.DataContext = new WaitingRoomPublicMatchViewModel();
        }
    }
}
