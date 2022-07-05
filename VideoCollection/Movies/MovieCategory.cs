using SQLite;

namespace VideoCollection.Movies
{
    public class MovieCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded List<int>
        public string MovieIds { get; set; }
        // Used for updating movies
        public bool IsChecked { get; set; }
    }
}
