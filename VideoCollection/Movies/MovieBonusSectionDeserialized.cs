using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;

namespace VideoCollection.Movies
{
    internal class MovieBonusSectionDeserialized
    {
        public string Name { get; set; }
        public SolidColorBrush Background { get; set; }

        public MovieBonusSectionDeserialized(MovieBonusSection section)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Name = section.Name;
            Color backgroundColor = jss.Deserialize<Color>(section.Background);
            Background = new SolidColorBrush(backgroundColor);
        }
    }
}
