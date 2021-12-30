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

        public MovieCategoryDeserialized(MovieCategory category)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Id = category.Id;
            Position = category.Position;
            Name = category.Name;
            List<Movie> moviesList = jss.Deserialize<List<Movie>>(category.Movies);
            List<MovieDeserialized> moviesDeserialized = new List<MovieDeserialized>();
            foreach (Movie movie in moviesList)
            {
                moviesDeserialized.Add(new MovieDeserialized(movie));
            }
            Movies = moviesDeserialized;
            IsChecked = category.IsChecked;
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
