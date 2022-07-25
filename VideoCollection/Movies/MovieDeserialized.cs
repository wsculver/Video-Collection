using System;
using System.Windows.Media;
using VideoCollection.Helpers;

namespace VideoCollection.Movies
{
    public class MovieDeserialized : IComparable
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
            if (App.movieThumbnails.ContainsKey(movie.Id))
            {
                Thumbnail = App.movieThumbnails[movie.Id];
            }
            else
            {
                Thumbnail = StaticHelpers.Base64ToImageSource(movie.Thumbnail);
            }
            ThumbnailVisibility = movie.ThumbnailVisibility;
            IsChecked = false;
        }

        public int CompareTo(object obj)
        {
            MovieDeserialized m = obj as MovieDeserialized;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Title, m.Title);
        }
    }
}
