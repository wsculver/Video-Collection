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
        public List<Movie> Movies { get; set; }
        public bool IsChecked { get; set; }

        public MovieCategoryDeserialized(int id, int position, string name, string movies, bool check)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Id = id;
            Position = position;
            Name = name;
            Movies = jss.Deserialize<List<Movie>>(movies);
            IsChecked = check;
        }

        public MovieCategoryDeserialized(int id, int position, string name, List<Movie> movies, bool check)
        {
            Id = id;
            Position = position;
            Name = name;
            Movies = movies;
            IsChecked = check;
        }
    }
}
