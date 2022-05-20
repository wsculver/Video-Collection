using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class ShowSeason : IComparable
    {
        [PrimaryKey, AutoIncrement]
        public int SeasonNumber { get; set; }
        // JSON encoded List<string>
        public string BonusSections { get; set; }
        // JSON encoded List<ShowBonusVideo>
        public string BonusVideos { get; set; }
       
        public int CompareTo(object obj)
        {
            ShowSeason m = obj as ShowSeason;
            return SeasonNumber.CompareTo(m.SeasonNumber);
        }
    }
}
