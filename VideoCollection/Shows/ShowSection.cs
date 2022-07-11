using Newtonsoft.Json;
using System;

namespace VideoCollection.Shows
{
    public class ShowSection : IComparable
    {
        public string Name { get; set; }
        // JSON encoded SolidColorBrush
        public string Background { get; set; }

        public ShowSection() { }

        public ShowSection(ShowSectionDeserialized section)
        {
            Name = section.Name;
            Background = JsonConvert.SerializeObject(section.Background);
        }

        public int CompareTo(object obj)
        {
            ShowSection s = obj as ShowSection;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Name, s.Name);
        }
    }
}
