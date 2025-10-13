using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GuessMyMessClient.Model
{
    public class AvatarModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] ImageData { get; set; }
        public BitmapImage ImageSource { get; set; }
    }
}
