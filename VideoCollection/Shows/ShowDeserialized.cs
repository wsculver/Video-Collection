﻿using System.Windows.Media;
using Newtonsoft.Json;
using System;
using VideoCollection.Helpers;

namespace VideoCollection.Shows
{
    public class ShowDeserialized : IComparable
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
            if (App.showThumbnails.ContainsKey(show.Id))
            {
                Thumbnail = App.showThumbnails[show.Id];
            } 
            else
            {
                Thumbnail = StaticHelpers.Base64ToImageSource(show.Thumbnail);
            }
            ThumbnailVisibility = show.ThumbnailVisibility;
            ShowVideo nextEpisode = JsonConvert.DeserializeObject<ShowVideo>(show.NextEpisode);
            NextEpisode = new ShowVideoDeserialized(nextEpisode);
            IsChecked = false;
        }

        public int CompareTo(object obj)
        {
            ShowDeserialized s = obj as ShowDeserialized;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Title, s.Title);
        }
    }
}
