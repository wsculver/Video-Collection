using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VideoCollection.Movies
{
    internal class MovieCategoryDeserialized
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public List<MovieDeserialized> Movies { get; set; }
        public bool IsChecked { get; set; }

        public MovieCategoryDeserialized(int id, int position, string name, string movies, bool check)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Id = id;
            Position = position;
            Name = name;
            List<Movie> moviesList = jss.Deserialize<List<Movie>>(movies);
            List<MovieDeserialized> moviesDeserialized = new List<MovieDeserialized>();
            foreach (Movie movie in moviesList)
            {
                moviesDeserialized.Add(new MovieDeserialized(movie.Id, movie.Title, movie.Thumbnail, movie.MovieFilePath, movie.BonusFolderPath, movie.BonusVideos, movie.Categories, false));
            }
            Movies = moviesDeserialized;
            IsChecked = check;
        }

        public MovieCategoryDeserialized(int id, int position, string name, List<MovieDeserialized> movies, bool check)
        {
            Id = id;
            Position = position;
            Name = name;
            Movies = movies;
            IsChecked = check;
        }
    }
}
