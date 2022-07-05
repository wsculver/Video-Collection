using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public string ShowTitle { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public string CommentariesSerialized { get; set; }
        public List<ShowVideoDeserialized> Commentaries { get; set; }
        public string DeletedScenesSerialized { get; set; }
        public List<ShowVideoDeserialized> DeletedScenes { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        public string SubtitlesSerialized { get; set; }
        public List<SubtitleSegment> Subtitles { get; set; }
        public Tuple<int, int> NextEpisode { get; set; }
        public string NextEpisodeSerialized { get; set; }
        public bool IsBonusVideo { get; set; }

        public ShowVideoDeserialized(ShowVideo video)
        {
            SeasonNumber = video.SeasonNumber;
            EpisodeNumber = video.EpisodeNumber;
            Title = video.Title;
            ShowTitle = video.ShowTitle;
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
            List<ShowVideo> deletedScenes = JsonConvert.DeserializeObject<List<ShowVideo>>(video.DeletedScenes);
            if (deletedScenes != null)
            {
                List<ShowVideoDeserialized> deletedScenesDeserialized = new List<ShowVideoDeserialized>();
                foreach (ShowVideo delScene in deletedScenes)
                {
                    deletedScenesDeserialized.Add(new ShowVideoDeserialized(delScene));
                }
                DeletedScenes = deletedScenesDeserialized;
            }
            else
            {
                DeletedScenes = null;
            }
            Section = video.Section;
            Runtime = video.Runtime;
            SubtitlesSerialized = video.Subtitles;
            Subtitles = JsonConvert.DeserializeObject<List<SubtitleSegment>>(video.Subtitles);
            NextEpisodeSerialized = video.NextEpisode;
            NextEpisode = JsonConvert.DeserializeObject<Tuple<int, int>>(video.NextEpisode);
            IsBonusVideo = video.IsBonusVideo;
        }

        public int CompareTo(object obj)
        {
            ShowVideoDeserialized video = obj as ShowVideoDeserialized;
            return EpisodeNumber.CompareTo(video.EpisodeNumber);
        }
    }
}
