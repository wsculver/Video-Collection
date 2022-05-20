using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class ShowCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded List<Show>
        public string Shows { get; set; }
        // Used for updating movies
        public bool IsChecked { get; set; }
    }
}
