using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Movies
{
    internal class Movie
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string MovieFilePath { get; set; }
        // JSON encoded List<string>
        public string Categories { get; set; }
        // Used for editing categories
        public bool IsChecked { get; set; }
    }
}
