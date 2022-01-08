using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VideoCollection.Popups;

namespace VideoCollection
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string path = Directory.GetCurrentDirectory();
        static string databaseFolderPath = Path.Combine(path, "Database");
        static string databaseName = "VideoCollection.db";
        public static string databasePath = Path.Combine(databaseFolderPath, databaseName);
        public static VideoPlayer videoPlayer = null;

        private void Application_Activated(object sender, EventArgs e)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Topmost = true;
            }
        }

        private void Application_Deactivated(object sender, EventArgs e)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Topmost = false;
            }
        }
    }
}
