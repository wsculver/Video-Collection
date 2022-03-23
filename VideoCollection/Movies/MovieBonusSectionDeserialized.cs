using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VideoCollection.Movies
{
    public class MovieBonusSectionDeserialized
    {
        public string Name { get; set; }
        public SolidColorBrush Background { get; set; }

        public MovieBonusSectionDeserialized(MovieBonusSection section)
        {
            Name = section.Name;
            Color backgroundColor = JsonConvert.DeserializeObject<Color>(section.Background);
            Background = new SolidColorBrush(backgroundColor);
        }
    }
}
