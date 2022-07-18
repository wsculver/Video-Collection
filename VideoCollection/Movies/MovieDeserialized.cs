using System;
using System.Windows.Media;

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
            Thumbnail = App.movieThumbnails[movie.Id];
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
