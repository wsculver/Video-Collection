using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.Popups;
using VideoCollection.Shows;

namespace VideoCollection
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string path = Directory.GetCurrentDirectory();
        static string databaseFolderPath = Path.Combine(path, "Database");
        static string databaseName = "VideoCollection.db";
        public static string databasePath = Path.Combine(databaseFolderPath, databaseName);
        public static VideoPlayer videoPlayer = null;
        public static double dpiWidthFactor = 1.0;
        public static double dpiHeightFactor = 1.0;
        public static ConcurrentDictionary<int, ImageSource> movieThumbnails = new ConcurrentDictionary<int, ImageSource>();
        public static ConcurrentDictionary<int, ImageSource> showThumbnails = new ConcurrentDictionary<int, ImageSource>();

        private void Application_Activated(object sender, EventArgs e)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Topmost = true;
            }

            using (SQLiteConnection connection = new SQLiteConnection(databasePath))
            {
                List<Movie> movies = connection.Table<Movie>().ToList();
                Parallel.ForEach(movies, movie =>
                {
                    var thumbnail = StaticHelpers.Base64ToImageSource(movie.Thumbnail);
                    thumbnail.Freeze();
                    movieThumbnails[movie.Id] = thumbnail;
                });

                List<Show> shows = connection.Table<Show>().ToList();
                Parallel.ForEach(shows, show =>
                {
                    var thumbnail = StaticHelpers.Base64ToImageSource(show.Thumbnail);
                    thumbnail.Freeze();
                    showThumbnails[show.Id] = thumbnail;
                });
            }
        }

        private void Application_Deactivated(object sender, EventArgs e)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Topmost = false;
            }
        }
    }
}
