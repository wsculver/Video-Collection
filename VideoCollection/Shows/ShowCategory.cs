using SQLite;

namespace VideoCollection.Shows
{
    public class ShowCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded SortedSet<int>
        public string ShowIds { get; set; }
    }
}
