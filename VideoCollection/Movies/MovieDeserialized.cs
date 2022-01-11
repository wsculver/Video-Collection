using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using VideoCollection.Helpers;
using VideoCollection.Subtitles;
using VideoCollection.Popups;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using VideoCollection.Database;

namespace VideoCollection.Movies
{
    public class MovieDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string MovieFolderPath { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string MovieFilePath { get; set; }
        public string Runtime { get; set; }
        public List<MovieBonusSectionDeserialized> BonusSections { get; set; }
        public List<MovieBonusVideoDeserialized> BonusVideos { get; set; }
        public string Rating { get; set; }
        public List<string> Categories { get; set; }
        public List<SubtitleSegment> Subtitles { get; set; }
        public bool IsChecked { get; set; }

        public MovieDeserialized(Movie movie)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;
            Id = movie.Id;
            Title = movie.Title;
            MovieFolderPath = movie.MovieFolderPath;
            if (!File.Exists(movie.MovieFilePath))
            {
                DatabaseFunctions.DeleteMovie(movie);
                throw new Exception("Could not find the movie file for " + Title + ". The movie was deleted from the database. To add the movie again you will need to click NEW MOVIE.");
            }
            MovieFilePath = movie.MovieFilePath;
            Thumbnail = StaticHelpers.Base64ToImageSource(movie.Thumbnail);
            List<MovieBonusSection> movieBonusSections = jss.Deserialize<List<MovieBonusSection>>(movie.BonusSections);
            List<MovieBonusSectionDeserialized> movieBonusSectionsDeserialized = new List<MovieBonusSectionDeserialized>();
            foreach (MovieBonusSection section in movieBonusSections)
            {
                movieBonusSectionsDeserialized.Add(new MovieBonusSectionDeserialized(section));
            }
            BonusSections = movieBonusSectionsDeserialized;
            List<MovieBonusVideo> movieBonusVideos = jss.Deserialize<List<MovieBonusVideo>>(movie.BonusVideos);
            List<MovieBonusVideoDeserialized> movieBonusVideosDeserialized = new List<MovieBonusVideoDeserialized>();
            foreach (MovieBonusVideo video in movieBonusVideos)
            {
                movieBonusVideosDeserialized.Add(new MovieBonusVideoDeserialized(video));
            }
            BonusVideos = movieBonusVideosDeserialized;
            Runtime = movie.Runtime;
            Rating = movie.Rating;
            Categories = jss.Deserialize<List<string>>(movie.Categories);
            Subtitles = jss.Deserialize<List<SubtitleSegment>>(movie.Subtitles);
            IsChecked = movie.IsChecked;
        }
    }
}
