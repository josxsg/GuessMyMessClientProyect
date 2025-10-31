using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using GuessMyMessClient.ViewModel;

namespace GuessMyMessClient.Model
{
    public class AvatarModel : ViewModelBase
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] ImageData { get; set; }
        public BitmapImage ImageSource { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}
