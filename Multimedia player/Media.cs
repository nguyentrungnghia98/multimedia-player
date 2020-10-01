using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Multimedia_player
{
    class Media : INotifyPropertyChanged
    {
        public MediaPlayer media = new MediaPlayer();
        public String Path;
        public String Playlist;
        public Boolean isPlaying = false;
        private TagLib.File tagFile;

        public Media(String path, String playlist)
        {
            Path = path;
            if (path == null) return;
            Icon = "Images/play-white.png";
            media.Open(new Uri(path));
            tagFile = TagLib.File.Create(path);
            Playlist = playlist;
        }

        public void Play()
        {
            //media.Position = TimeSpan.FromSeconds(30);
            //media.ScrubbingEnabled = true;
            media.Play();
            isPlaying = true;
            Icon = "Images/pause-white.png";
        }

        public void Stop()
        {
            media.Stop();
            isPlaying = false;
            Icon = "Images/play-white.png";
        }

        public void Pause()
        {
            media.Pause();
            isPlaying = false;
            Icon = "Images/play-white.png";
        }

        public String Icon
        {
            get;set;
        }

        public String currentPosition
        {
            get {
                return media.Position.ToString(@"hh\:mm\:ss");
            }
        }

        public String length
        {
            get
            {
                return media.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            }
        }

        public String Type
        {
            get
            {
                char[] spearator = { '/' };

                String[] arr = tagFile.MimeType.Split(spearator, StringSplitOptions.None);
                if (arr.Length == 1) return arr[0];
                return arr[1];
            }
        }

        public String TypeMedia
        {
            get
            {
                if (Type.ToLower() == "ogg" || Type.ToLower() == "mp3" || Type.ToLower() == "wav")
                {
                    return "Audio";
                }
                else
                {
                    return "Video";
                }
            }
        }

        public String Title
        {
            get
            {
                if (tagFile.Tag.Title != null) return tagFile.Tag.Title;
                //E:/a/a.mp3
                var tokens = tagFile.Name.Split(new string[] { "/","\\" },
                StringSplitOptions.None);
                if (tokens.Length == 0) return "Unknown";
                String name = tokens[tokens.Length - 1];
                tokens = name.Split(new string[] { "." },
                StringSplitOptions.None);
                if (tokens.Length == 0) return "Unknown";
                return tokens[0];
            }
        }

        public String Artist
        {
            get
            {
                return tagFile.Tag.FirstPerformer != null? tagFile.Tag.FirstPerformer: "Unknown";
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
