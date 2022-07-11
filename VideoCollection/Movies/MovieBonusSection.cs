using System;

namespace VideoCollection.Movies
{
    public class MovieBonusSection : IComparable
    {
        public string Name { get; set; }
        // JSON encoded SolidColorBrush
        public string Background { get; set; }

        public int CompareTo(object obj)
        {
            MovieBonusSection m = obj as MovieBonusSection;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Name, m.Name);
        }
    }
}
