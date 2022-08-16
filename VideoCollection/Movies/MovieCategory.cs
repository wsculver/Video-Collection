using SQLite;

namespace VideoCollection.Movies
{
    public class MovieCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded SortedSet<int>
        public string MovieIds { get; set; }
    }
}
