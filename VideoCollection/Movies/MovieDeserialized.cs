using System.Windows.Media;
using VideoCollection.Helpers;

namespace VideoCollection.Movies
{
    public class MovieDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string ThumbnailVisibility { get; set; }
        public bool IsChecked { get; set; }

        public MovieDeserialized(Movie movie)
        {
            Id = movie.Id;
            Title = movie.Title;
            Thumbnail = StaticHelpers.Base64ToImageSource(movie.Thumbnail);
            ThumbnailVisibility = movie.ThumbnailVisibility;
            IsChecked = movie.IsChecked;
        }
    }
}
