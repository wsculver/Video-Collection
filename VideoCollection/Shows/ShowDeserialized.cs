using System.Windows.Media;
using VideoCollection.Helpers;
using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class ShowDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string ThumbnailVisibility { get; set; }
        public ShowVideoDeserialized NextEpisode { get; set; }
        public bool IsChecked { get; set; }

        public ShowDeserialized(Show show)
        {
            Id = show.Id;
            Title = show.Title;
            Thumbnail = StaticHelpers.Base64ToImageSource(show.Thumbnail);
            ThumbnailVisibility = show.ThumbnailVisibility;
            ShowVideo nextEpisode = JsonConvert.DeserializeObject<ShowVideo>(show.NextEpisode);
            NextEpisode = new ShowVideoDeserialized(nextEpisode);
            IsChecked = show.IsChecked;
        }
    }
}
