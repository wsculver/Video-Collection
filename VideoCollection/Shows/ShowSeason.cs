using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class ShowSeason : IComparable
    {
        [PrimaryKey, AutoIncrement]
        public int SeasonNumber { get; set; }
        public string SeasonName { get; set; }
        // JSON encoded List<string>
        public string Sections { get; set; }
        // JSON encoded List<ShowVideo>
        public string Videos { get; set; }
       
        public ShowSeason() { }

        public ShowSeason(ShowSeasonDeserialized season)
        {
            SeasonNumber = season.SeasonNumber;
            SeasonName = "Season " + (season.SeasonNumber).ToString();
            List<ShowSection> sections = new List<ShowSection>();
            foreach(var section in season.Sections)
            {
                sections.Add(new ShowSection(section));
            }
            Sections = JsonConvert.SerializeObject(sections);
            List<ShowVideo> videos = new List<ShowVideo>();
            foreach (var video in season.Videos) 
            {
                videos.Add(new ShowVideo(video));
            }
            Videos = JsonConvert.SerializeObject(videos);
        }

        public int CompareTo(object obj)
        {
            ShowSeason m = obj as ShowSeason;
            return SeasonNumber.CompareTo(m.SeasonNumber);
        }
    }
}
