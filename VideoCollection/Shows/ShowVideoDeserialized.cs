using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using VideoCollection.Helpers;
using VideoCollection.Subtitles;

namespace VideoCollection.Shows
{
    public class ShowVideoDeserialized : IComparable
    {
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public string CommentariesSerialized { get; set; }
        public List<ShowVideoDeserialized> Commentaries { get; set; }
        public string DeletedScenesSerialized { get; set; }
        public ShowVideoDeserialized DeletedScenes { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        public string SubtitlesSerialized { get; set; }
        public List<SubtitleSegment> Subtitles { get; set; }
        public Tuple<int, int> NextEpisode { get; set; }
        public bool IsBonusVideo { get; set; }

        public ShowVideoDeserialized(ShowVideo video)
        {
            SeasonNumber = video.SeasonNumber;
            EpisodeNumber = video.EpisodeNumber;
            Title = video.Title;
            Thumbnail = StaticHelpers.Base64ToImageSource(video.Thumbnail);
            FilePath = video.FilePath;
            CommentariesSerialized = video.Commentaries;
            List<ShowVideo> commentaries = JsonConvert.DeserializeObject<List<ShowVideo>>(video.Commentaries);
            if (commentaries != null)
            {
                List<ShowVideoDeserialized> commentariesDeserialized = new List<ShowVideoDeserialized>();
                foreach (ShowVideo commentary in commentaries)
                {
                    commentariesDeserialized.Add(new ShowVideoDeserialized(commentary));
                }
                Commentaries = commentariesDeserialized;
            }
            else
            {
                Commentaries = null;
            }
            DeletedScenesSerialized = video.DeletedScenes;
            ShowVideo deletedScenes = JsonConvert.DeserializeObject<ShowVideo>(video.DeletedScenes);
            if (deletedScenes != null)
            {
                DeletedScenes = new ShowVideoDeserialized(deletedScenes);
            }
            else
            {
                DeletedScenes = null;
            }
            Section = video.Section;
            Runtime = video.Runtime;
            SubtitlesSerialized = video.Subtitles;
            Subtitles = JsonConvert.DeserializeObject<List<SubtitleSegment>>(video.Subtitles);
            NextEpisode = JsonConvert.DeserializeObject<Tuple<int, int>>(video.NextEpisode);
            IsBonusVideo = video.IsBonusVideo;
        }

        public ShowVideoDeserialized(string title, string filePath, string runtime, string subtitles, string commentaries, string deletedScenes, bool isBonusVideo)
        {
            Title = title;
            FilePath = filePath;
            Runtime = runtime;
            Subtitles = JsonConvert.DeserializeObject<List<SubtitleSegment>>(subtitles);
            List<ShowVideo> videoCommentaries = JsonConvert.DeserializeObject<List<ShowVideo>>(commentaries);
            if (videoCommentaries != null)
            {
                List<ShowVideoDeserialized> commentariesDeserialized = new List<ShowVideoDeserialized>();
                foreach (ShowVideo commentary in videoCommentaries)
                {
                    commentariesDeserialized.Add(new ShowVideoDeserialized(commentary));
                }
                Commentaries = commentariesDeserialized;
            }
            else
            {
                Commentaries = null;
            }
            ShowVideo videoDeletedScenes = JsonConvert.DeserializeObject<ShowVideo>(deletedScenes);
            if (videoDeletedScenes != null)
            {
                DeletedScenes = new ShowVideoDeserialized(videoDeletedScenes);
            }
            else
            {
                DeletedScenes = null;
            }
            IsBonusVideo = isBonusVideo;
        }

        public int CompareTo(object obj)
        {
            ShowVideoDeserialized video = obj as ShowVideoDeserialized;
            return EpisodeNumber.CompareTo(video.EpisodeNumber);
        }
    }
}
