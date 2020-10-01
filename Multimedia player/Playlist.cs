using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimedia_player
{
    class Playlist: INotifyPropertyChanged
    {
        public BindingList<Media> Medias { get; set; }
        public string Name { get; set; }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Total
        {
            get
            {
                return $"{Medias.Count} songs";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
