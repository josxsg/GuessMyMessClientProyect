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
using GuessMyMessClient.ViewModel.Session;
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
            this.Closed += WaitingRoom_Closed;
        }

        private void WaitingRoom_Closed(object sender, EventArgs e)
        {
            if (this.DataContext is WaitingRoomViewModelBase vm)
            {
                vm.CleanUp();
            }
            this.Closed -= WaitingRoom_Closed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
