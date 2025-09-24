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
using GuessMyMessClient.ViewModel.Lobby;    

namespace GuessMyMessClient.View.Lobby
{
    /// <summary>
    /// Lógica de interacción para SelectAvatar.xaml
    /// </summary>
    public partial class SelectAvatar : Window
    {
        public SelectAvatar()
        {
            InitializeComponent();
            this.DataContext = new SelectAvatarViewModel();
        }
    }
}
