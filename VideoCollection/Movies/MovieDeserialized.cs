﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using VideoCollection.Helpers;

namespace VideoCollection.Movies
{
    public class MovieDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string MovieFolderPath { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string MovieFilePath { get; set; }
        public string Runtime { get; set; }
        public List<MovieBonusSectionDeserialized> BonusSections { get; set; }
        public List<MovieBonusVideoDeserialized> BonusVideos { get; set; }
        public string Rating { get; set; }
        public List<string> Categories { get; set; }
        public bool IsChecked { get; set; }

        public MovieDeserialized(Movie movie)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Id = movie.Id;
            Title = movie.Title;
            MovieFolderPath = movie.MovieFolderPath;
            Thumbnail = StaticHelpers.BitmapFromUri(new Uri(new Uri(Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar), movie.Thumbnail));
            List<MovieBonusSection> movieBonusSections = jss.Deserialize<List<MovieBonusSection>>(movie.BonusSections);
            List<MovieBonusSectionDeserialized> movieBonusSectionsDeserialized = new List<MovieBonusSectionDeserialized>();
            foreach (MovieBonusSection section in movieBonusSections)
            {
                movieBonusSectionsDeserialized.Add(new MovieBonusSectionDeserialized(section));
            }
            BonusSections = movieBonusSectionsDeserialized;
            List<MovieBonusVideo> movieBonusVideos = jss.Deserialize<List<MovieBonusVideo>>(movie.BonusVideos);
            List<MovieBonusVideoDeserialized> movieBonusVideosDeserialized = new List<MovieBonusVideoDeserialized>();
            foreach (MovieBonusVideo video in movieBonusVideos)
            {
                movieBonusVideosDeserialized.Add(new MovieBonusVideoDeserialized(video));
            }
            BonusVideos = movieBonusVideosDeserialized;
            MovieFilePath = movie.MovieFilePath;
            Runtime = movie.Runtime;
            Rating = movie.Rating;
            Categories = jss.Deserialize<List<string>>(movie.Categories);
            IsChecked = movie.IsChecked;
        }
    }
}
