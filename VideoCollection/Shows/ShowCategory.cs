using SQLite;

namespace VideoCollection.Shows
{
    public class ShowCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        // JSON encoded List<int>
        public string ShowIds { get; set; }
        // Used for updating movies
        public bool IsChecked { get; set; }
    }
}
