using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class ShowSection
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
    }
}
