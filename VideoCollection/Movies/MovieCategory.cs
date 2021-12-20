using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Movies
{
    internal class MovieCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded List<Movie>
        public string Movies { get; set; }
        // Used for updating movies
        public bool IsChecked { get; set; }
    }
}
