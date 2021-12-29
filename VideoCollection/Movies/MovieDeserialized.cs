using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace VideoCollection.Movies
{
    internal class MovieDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string MovieFilePath { get; set; }
        public string BonusFolderPath { get; set; }
        public List<MovieBonusVideoDeserialized> BonusVideos { get; set; }
        public List<string> Categories { get; set; }
        public bool IsChecked { get; set; }

        public MovieDeserialized(int id, string title, string thumbnail, string filePath, string bonusFolderPath, string bonusVideos, string categories, bool check)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Id = id;
            Title = title;
            Thumbnail = BitmapFromUri(new Uri(new Uri(Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar), thumbnail));
            BonusFolderPath = bonusFolderPath;
            List<MovieBonusVideo> movieBonusVideos = jss.Deserialize<List<MovieBonusVideo>>(bonusVideos);
            List<MovieBonusVideoDeserialized> movieBonusVideosDeserialized = new List<MovieBonusVideoDeserialized>();
            foreach (MovieBonusVideo video in movieBonusVideos)
            {
                movieBonusVideosDeserialized.Add(new MovieBonusVideoDeserialized(video.Title, video.Thumbnail, video.FilePath));
            }
            BonusVideos = movieBonusVideosDeserialized;
            MovieFilePath = filePath;
            Categories = jss.Deserialize<List<string>>(categories);
            IsChecked = check;
        }

        // Convert a Uri into an ImageSource
        private ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
    }
}
