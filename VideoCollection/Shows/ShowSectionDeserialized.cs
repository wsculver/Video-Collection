using Newtonsoft.Json;
using System.Windows.Media;

namespace VideoCollection.Shows
{
    public class ShowSectionDeserialized
    {
        public string Name { get; set; }
        public SolidColorBrush Background { get; set; }

        public ShowSectionDeserialized(ShowSection section)
        {
            Name = section.Name;
            Color backgroundColor = JsonConvert.DeserializeObject<Color>(section.Background);
            Background = new SolidColorBrush(backgroundColor);
            Background.Freeze();
        }
    }
}
