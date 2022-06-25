using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoCollection.Helpers;

namespace VideoCollection.Shows
{
    public class ShowVideo : IComparable
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string ShowTitle { get; set; }
        public string Thumbnail { get; set; }
        public string FilePath { get; set; }
        // JSON encoded List<ShowVideo>
        public string Commentaries { get; set; }
        // JSON encoded List<ShowVideo>
        public string DeletedScenes { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<SubtitleSegment>
        public string Subtitles { get; set; }
        // JSON encoded Tuple<int, int>
        public string NextEpisode { get; set; }
        public bool IsBonusVideo { get; set; }

        public ShowVideo() { }

        public ShowVideo(ShowVideoDeserialized video)
        {
            SeasonNumber = video.SeasonNumber;
            EpisodeNumber = video.EpisodeNumber;
            Title = video.Title;
            ShowTitle = video.ShowTitle;
            Thumbnail = StaticHelpers.ImageSourceToBase64(video.Thumbnail);
            FilePath = video.FilePath;
            if (video.Commentaries != null)
            {
                List<ShowVideo> commentaries = new List<ShowVideo>();
                foreach (var commentary in video.Commentaries)
                {
                    commentaries.Add(new ShowVideo(commentary));
                }
                Commentaries = JsonConvert.SerializeObject(commentaries);
            }
            else
            {
                Commentaries = "";
            }
            if (video.DeletedScenes != null)
            {
                List<ShowVideo> deletedScenes = new List<ShowVideo>();
                foreach (var delScene in video.DeletedScenes)
                {
                    deletedScenes.Add(new ShowVideo(delScene));
                }
                DeletedScenes = JsonConvert.SerializeObject(deletedScenes);
            }
            else
            {
                DeletedScenes = "";
            }
            Section = video.Section;
            Runtime = video.Runtime;
            Subtitles = JsonConvert.SerializeObject(video.Subtitles);
            NextEpisode = JsonConvert.SerializeObject(video.NextEpisode);
            IsBonusVideo = video.IsBonusVideo;
        }

        public int CompareTo(object obj)
        {
            ShowVideo video = obj as ShowVideo;
            return EpisodeNumber.CompareTo(video.EpisodeNumber);
        }
    }
}
