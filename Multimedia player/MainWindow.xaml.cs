using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Multimedia_player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        BindingList<Playlist> playlists = new BindingList<Playlist>();
        int selectedPlaylist = 0;
        int playingSongIndex = 0;
        List<PrevMedia> prevMedias = new List<PrevMedia> { };
        Media playingMedia;
        Timer timer;
        String[] repeatModes = { "off", "all", "one" };
        bool isShuffle = false;
        int repeat = 0; // off
        int isPlayEnd = 0;
        private IKeyboardMouseEvents _hook;

        class PrevMedia
        {
            public Media Media { get; set; }
            public int Index { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            getPlaylists();

            currentPosition.AddHandler(MouseLeftButtonUpEvent,
                      new MouseButtonEventHandler(CurrentPosition_MouseLeftButtonUp),
                      true);

            volume.AddHandler(MouseLeftButtonUpEvent,
                      new MouseButtonEventHandler(Volume_MouseLeftButtonUp),
                      true);

            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            // Dang ky su kien hook
            _hook = Hook.GlobalEvents();
            _hook.KeyUp += KeyUp_hook;

        }

        private void KeyUp_hook(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //if (e.Control && e.Shift && (e.KeyCode == System.Windows.Forms.Keys.E))
            //{
            //    //System.Windows.MessageBox.Show("Ctrl + Shift + E pressed"); ;
            //    _lastIndex++;
            //    PlaySelectedIndex(_lastIndex);
            //}
            if (e.KeyCode == System.Windows.Forms.Keys.Space || e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                togglePlay();
            }
            if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.Right))
            {
                playNext();
            }
            if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.Left))
            {
                playPrev();
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Window_Closing");
            _hook.KeyUp -= KeyUp_hook;
            _hook.Dispose();

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                playingMedia.RaisePropertyChanged("currentPosition");
                var totalSeconds = playingMedia.media.NaturalDuration.TimeSpan.TotalSeconds;
                var currentSeconds = playingMedia.media.Position.TotalSeconds;
                currentPosition.Value = (currentSeconds / totalSeconds) * 100;
                Debug.WriteLine(isPlayEnd);
                if(totalSeconds == currentSeconds)
                {
                    isPlayEnd++;
                    //repeat one
                    if (repeat == 2)
                    {
                        playingMedia.media.Position = TimeSpan.FromSeconds(0);
                        currentPosition.Value = 0;
                    }
                    else
                    {
                        if(isPlayEnd == 1) playNext();
                    }
                    
                }
                else
                {
                    isPlayEnd = 0;
                }
            });

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("file.txt", false, Encoding.UTF8))
            {
                file.WriteLine(playingMedia.Path + "|" + playingMedia.Playlist + "|" + playingSongIndex);
                foreach (Playlist playlist in playlists)
                {
                    String line = playlist.Name;
                    foreach(Media media in playlist.Medias)
                    {
                        line += "|" + media.Path;
                    }
                    file.WriteLine(line);
                }
            }

        }

        private void getLastSongPlaying(string path, string playlistName, string index)
        {
            if(path != "empty" || System.IO.File.Exists(path))
            {
                playingMedia = new Media(path, playlistName);
                this.DataContext = playingMedia;
                playingMedia.media.Volume = volume.Value / 100;

                
                int res = 0;
                if (Int32.TryParse(index, out res))
                    playingSongIndex = res;


                var sb = new Storyboard();
                var ta = new ThicknessAnimation();
                ta.BeginTime = new TimeSpan(0);
                ta.SetValue(Storyboard.TargetNameProperty, txtPlayingTitle.Name);
                Storyboard.SetTargetProperty(ta, new PropertyPath(MarginProperty));

                ta.From = new Thickness(0, 0, 0, 0);
                var a = txtPlayingTitle;
                ta.To = new Thickness(-200, 0, 0, 0);
                ta.Duration = new Duration(TimeSpan.FromSeconds(3));
                ta.AutoReverse = true;
                ta.RepeatBehavior = RepeatBehavior.Forever;

                sb.Children.Add(ta);
                sb.Begin(this);
            }
            else
            {
                playDefaultMusic();
            }
            
        }

        private void playDefaultMusic()
        {
            if (System.IO.File.Exists("default.mp3"))
            {
                playingMedia = new Media(AppDomain.CurrentDomain.BaseDirectory + "/default.mp3", "*");
                this.DataContext = playingMedia;
                playingMedia.media.Volume = volume.Value / 100;
                

                var sb = new Storyboard();
                var ta = new ThicknessAnimation();
                ta.BeginTime = new TimeSpan(0);
                ta.SetValue(Storyboard.TargetNameProperty, txtPlayingTitle.Name);
                Storyboard.SetTargetProperty(ta, new PropertyPath(MarginProperty));

                ta.From = new Thickness(0, 0, 0, 0);
                var a = txtPlayingTitle;
                ta.To = new Thickness(-200, 0, 0, 0);
                ta.Duration = new Duration(TimeSpan.FromSeconds(3));
                ta.AutoReverse = true;
                ta.RepeatBehavior = RepeatBehavior.Forever;

                sb.Children.Add(ta);
                sb.Begin(this);
            }
        }

        private void getPlaylists()
        {
            

            playlists = new BindingList<Playlist>();

            if (System.IO.File.Exists("file.txt"))
            {
                try
                {
                    using (StreamReader reader = new StreamReader("file.txt", Encoding.Default, true))
                    {
                        string firstLine = reader.ReadLine();
                        if (String.IsNullOrEmpty(firstLine))
                        {
                            playDefaultMusic();
                            playlistsListBox.ItemsSource = playlists;
                            return;
                        }
                        var tokensLastsong = firstLine.Split(new string[] { "|" }, StringSplitOptions.None);

                        string currentLine;
                        while ((currentLine = reader.ReadLine()) != null)
                        {
                            var tokens = currentLine.Split(new string[] { "|" }, StringSplitOptions.None);
                            string name = tokens[0];
                            if (!String.IsNullOrEmpty(name))
                            {
                                BindingList<Media> medias = new BindingList<Media>();
                                if (tokens.Length > 1)
                                {
                                    for (int i = 1; i < tokens.Length; i++)
                                    {
                                        if (!System.IO.File.Exists(tokens[i])) continue;
                                        Media media = new Media(tokens[i], name);
                                        medias.Add(media);
                                    }
                                }

                                Playlist playlist = new Playlist { Medias = medias, Name = name };
                                playlists.Add(playlist);
                            }

                        }
                        getLastSongPlaying(tokensLastsong[0], tokensLastsong[1], tokensLastsong[2]);

                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show("Read file save error!\n"+err.Message, "Error");
                    playDefaultMusic();
                }
            }
            else
            {
                playDefaultMusic();
            }

            playlistsListBox.ItemsSource = playlists;
            
        }

        private void updatePlaylistUI()
        {
            txtPlaylistName.Text = playlists[selectedPlaylist].Name;
            txtPlaylistTotal.Text = playlists[selectedPlaylist].Total;
            playlists[selectedPlaylist].RaisePropertyChanged("Total");
            playlists[selectedPlaylist].RaisePropertyChanged("Name");
        }

        private void openPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
                
            int selectedIndex = playlistsListBox.SelectedIndex;
            Debug.WriteLine("Delete line" + selectedIndex );
            if (selectedIndex >= 0)
            {
                selectedPlaylist = selectedIndex;
                updatePlaylistUI();
                songsListBox.ItemsSource = playlists[selectedPlaylist].Medias;
                viewPlaylist.Visibility = Visibility.Visible;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox.SelectedIndex == -1)
                foreach (object item in e.RemovedItems)
                {
                    listBox.SelectedItem = item;
                    break;
                }

        }

        private void CloseViewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            viewPlaylist.Visibility = Visibility.Hidden;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            addPlaylist.Visibility = Visibility.Visible;
        }

        private void CloseCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            addPlaylist.Visibility = Visibility.Hidden;
        }

        private void CreatePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            String name = txtNewNamePlaylist.Text;

            for(int i = 0; i < playlists.Count; i++)
                if(name == playlists[i].Name)
                {
                    MessageBox.Show("This name is already taken");
                    return;
                }

            Playlist playlist = new Playlist { Medias = new BindingList<Media>(), Name = name };
            playlists.Add(playlist);

            //playlistsListBox.ItemsSource = playlists;
            txtNewNamePlaylist.Text = "";
            addPlaylist.Visibility = Visibility.Hidden;
        }

        private BindingList<Media> OpenFile()
        {
            string filter = "Audio(*.ogg, *.mp3, *.wav)|*.ogg;*.mp3;*.wav;|Video (*.avi, *.mkv, *.mp4, *.flv)|*.avi;*.mkv;*.mp4;*.flv";
            var openDialog = new OpenFileDialog { Multiselect = true, CheckFileExists = true, CheckPathExists = true, Title = "Select video file", AddExtension = true, Filter = filter };
            if (openDialog.ShowDialog(this) == true)
            {
                var files = openDialog.FileNames;
                if (files == null || files.Length == 0) return null;
                BindingList<Media> medias = new BindingList<Media>();
                foreach (String path in files)
                {
                    medias.Add(new Media(path, playlists[selectedPlaylist].Name));
                }
                return medias;
            }
            return null;
        }

        private void AddSongToPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            BindingList<Media> newMedias = OpenFile();
            if (newMedias == null || newMedias.Count == 0) return;
            foreach (Media media in newMedias)
                playlists[selectedPlaylist].Medias.Add(media);

            updatePlaylistUI();           
        }

        private void PlayAllSongPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            
            if(playlists[selectedPlaylist].Medias.Count == 0)
            {
                MessageBox.Show("Please add a new song!");
                return;
            }
            

            PlaySongAtIndex(0);
        }

        private void RenamePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            RenameDialog inputDialog = new RenameDialog();
            if (inputDialog.ShowDialog() == true)
            {
                String newName = inputDialog.Answer;
                if (String.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Name must not be empty!");
                    return;
                }
                for (int i = 0; i < playlists.Count; i++)
                    if (newName == playlists[i].Name)
                    {
                        MessageBox.Show("This name is already taken");
                        return;
                    }
                playlists[selectedPlaylist].Name = newName;
                playlists[selectedPlaylist].RaisePropertyChanged("Name");
                txtPlaylistName.Text = playlists[selectedPlaylist].Name;
                MessageBox.Show("Success!");
            }

        }

        private void ExportPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFilePath = "";
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save an Txt";
            
            saveFileDialog.FileName = $"Playlist_{playlists[selectedPlaylist].Name}.txt";
            saveFileDialog.Filter = "Text File | *.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SelectedFilePath = saveFileDialog.FileName;
                    if (String.IsNullOrEmpty(SelectedFilePath)) return;

                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(SelectedFilePath, false, Encoding.UTF8))
                    {
                        String line = playlists[selectedPlaylist].Name;
                        foreach (Media media in playlists[selectedPlaylist].Medias)
                        {
                            line += "|" + media.Path;
                        }
                        file.WriteLine(line);
                        
                    }
                    MessageBox.Show("Playlist is saved");
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, "Error");
                }

            }
        }

        private void PlaySongAtIndex(int index)
        {
            playingSongIndex = index;
            playingMedia = playlists[selectedPlaylist].Medias[playingSongIndex];
            playingMedia.media.Volume = volume.Value / 100;
            playingMedia.Play();
            this.DataContext = playingMedia;

            prevMedias = new List<PrevMedia> { };
        }

        private void PlaySongBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playingMedia != null) playingMedia.media.Stop();

            int selectedIndex = songsListBox.SelectedIndex;
            Debug.WriteLine("play line" + selectedIndex);
            if (selectedIndex >= 0)
            {
                PlaySongAtIndex(selectedIndex);
            }
        }

        private void DeleteMediaBtn_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = songsListBox.SelectedIndex;
            if (selectedIndex == playingSongIndex) playingMedia.Stop();
            if (selectedIndex >= 0) playlists[selectedPlaylist].Medias.RemoveAt(selectedIndex);
            updatePlaylistUI();
        }

        private void togglePlay()
        {
            if (playingMedia != null)
            {
                if (playingMedia.isPlaying)
                {
                    playingMedia.Pause();
                }
                else
                {
                    playingMedia.Play();
                }
            }
        }

        private void TogglePlayBtn_Click(object sender, RoutedEventArgs e)
        {
            togglePlay();
        }

        

        private void playMediaAtPlaylist(int index,Playlist playlist)
        {
            prevMedias.Add(new PrevMedia { Index = playingSongIndex, Media = playingMedia });

            playingSongIndex = index;
            playingMedia.Stop();
            playingMedia = playlist.Medias[playingSongIndex];
            playingMedia.media.Volume = volume.Value / 100;
            playingMedia.Play();
            this.DataContext = playingMedia;
        }

        private int getRandomIndex(Playlist currentPlaylist)
        {
            List<int> validIndex = new List<int> { };
            for(int i = 0; i < currentPlaylist.Medias.Count; i++)
            {
                bool check = true;
                for(int j = 0; j < prevMedias.Count; j++)
                {
                    if(i == prevMedias[j].Index)
                    {
                        check = false;
                        break;
                    }
                }
                if (check && i != playingSongIndex) validIndex.Add(i);
            }
            if (validIndex.Count == 0) return -1;
            Random rnd = new Random();
            return validIndex[rnd.Next(validIndex.Count)];
        }

        private void playNext()
        {
            String currentPlaylistName = playingMedia.Playlist;
            Playlist _currentPlaylist = null;
            foreach (Playlist playlist in playlists)
            {
                if (playlist.Name == currentPlaylistName)
                    _currentPlaylist = playlist;
            }
            if (_currentPlaylist != null)
            {
                if (!isShuffle)
                {
                    if (playingSongIndex < _currentPlaylist.Medias.Count - 1)
                    {
                        playMediaAtPlaylist(playingSongIndex + 1, _currentPlaylist);
                    }
                    else
                    {
                        //repeat all
                        if (repeat == 1 )
                        {
                            playMediaAtPlaylist(0, _currentPlaylist);
                        }
                        else
                        {
                            MessageBox.Show("Out of the list", "Warning");
                            return;
                        }
                    }
                }
                else
                {
                    //repeat all
                    if (repeat == 1)
                    {
                        Random rnd = new Random();
                        int nextIndex = rnd.Next(_currentPlaylist.Medias.Count);
                        playMediaAtPlaylist(nextIndex, _currentPlaylist);
                    }
                    else
                    {
                        int nextIndex = getRandomIndex(_currentPlaylist);
                        if (nextIndex == -1)
                        {
                            if (repeat == 1) playMediaAtPlaylist(0, _currentPlaylist);
                            else MessageBox.Show("Out of the list", "Warning");
                        }
                        else
                        {
                            playMediaAtPlaylist(nextIndex, _currentPlaylist);
                        }
                    }
                    

                }
                
            }
            else
            {
                MessageBox.Show("Playlist not found, select another playlist", "Warning");
            }
        }

        private void playPrev()
        {
            if (prevMedias.Count > 0)
            {
                PrevMedia prev = prevMedias[prevMedias.Count - 1];
                prevMedias.RemoveAt(prevMedias.Count - 1);
                
                playingSongIndex = prev.Index;
                playingMedia.Stop();
                playingMedia = prev.Media;
                playingMedia.media.Volume = volume.Value / 100;
                playingMedia.Play();
                this.DataContext = playingMedia;
            }
            else
            {
                MessageBox.Show("Out of the list", "Warning");
            }
        }

        private void PlayNextBtn_Click(object sender, RoutedEventArgs e)
        {
            playNext();
        }

        private void PlayPrevBtn_Click(object sender, RoutedEventArgs e)
        {

            playPrev();
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playingMedia != null) playingMedia.Stop();
        }

        private void ImportPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();
            if (screen.ShowDialog() == true)
            {
                var filename = screen.FileName;
                if (String.IsNullOrEmpty(filename)) return;
                using (StreamReader reader = new StreamReader(filename, Encoding.Default, true))
                {
                    string currentLine = reader.ReadLine();

                    var tokens = currentLine.Split(new string[] { "|" }, StringSplitOptions.None);
                    string name = tokens[0];
                    if (!String.IsNullOrEmpty(name))
                    {
                        BindingList<Media> medias = new BindingList<Media>();
                        if (tokens.Length > 1)
                        {
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                if (!System.IO.File.Exists(tokens[i])) continue;
                                Media media = new Media(tokens[i], name);
                                medias.Add(media);
                            }
                        }

                        for (int i = 0; i < playlists.Count; i++)
                            if (name == playlists[i].Name)
                            {
                                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"This playlist name: \"{name}\" is already taken. Do you want to rename it?", "Rename Confirmation", System.Windows.MessageBoxButton.YesNo);
                                if (messageBoxResult == MessageBoxResult.Yes)
                                {
                                    RenameDialog inputDialog = new RenameDialog();
                                    if (inputDialog.ShowDialog() == true)
                                    {
                                        String newName = inputDialog.Answer;
                                        if (String.IsNullOrEmpty(newName))
                                        {
                                            MessageBox.Show("Name must not be empty!");
                                            return;
                                        }
                                        for (int j = 0; j < playlists.Count; j++)
                                            if (newName == playlists[i].Name)
                                            {
                                                MessageBox.Show("This name is already taken");
                                                return;
                                            }

                                        Playlist _playlist = new Playlist { Medias = medias, Name = newName };
                                        playlists.Add(_playlist);
                                        MessageBox.Show("Playlist is added!");
                                    }
                                }
                                return;
                            }
                        

                        Playlist playlist = new Playlist { Medias = medias, Name = name };
                        playlists.Add(playlist);
                        MessageBox.Show("Playlist is added!");
                    }
                }
            }
        }

        private void DeletePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylist > -1 && selectedPlaylist < playlists.Count)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    playlists.RemoveAt(selectedPlaylist);
                    viewPlaylist.Visibility = Visibility.Hidden;
                }
            }
        }

        private void CurrentPosition_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            //var totalSeconds = playingMedia.media.NaturalDuration.TimeSpan.TotalSeconds;
            //var currentSeconds = playingMedia.media.Position.TotalSeconds;

            Debug.WriteLine("a  " + currentPosition.Value);
            playingMedia.media.Position = TimeSpan.FromSeconds(currentPosition.Value/100 * playingMedia.media.NaturalDuration.TimeSpan.TotalSeconds);
            playingMedia.RaisePropertyChanged("currentPosition");
        }

        private void Volume_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            playingMedia.media.Volume = volume.Value/100;
        }

        private void RepeatBtn_Click(object sender, RoutedEventArgs e)
        {
 
            repeat = (repeat + 1) % 3;
            repeatBtn.Header = "Repeat: " + repeatModes[repeat];
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            isShuffle = !isShuffle;
            shuffleBtn.Header = isShuffle ? "Shuffle: on" : "Shuffle: off";
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                //Your Logic
                e.Handled = true;
            }
        }
    }
}
