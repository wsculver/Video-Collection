using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using VideoCollection.Helpers;

namespace VideoCollection.Movies
{
    internal class MovieBonusVideoDeserialized
    {
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }

        public MovieBonusVideoDeserialized(string title, string base64String, string filePath)
        {
            Title = title;
            Thumbnail = StaticHelpers.Base64ToImageSource(base64String);
            FilePath = filePath;
        }
    }
}
