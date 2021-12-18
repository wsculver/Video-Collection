using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VideoCollection
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        static string databaseFolderPath = System.IO.Path.Combine(path, "Database");
        static string databaseName = "VideoCollection.db";
        public static string databasePath = System.IO.Path.Combine(databaseFolderPath, databaseName);
    }
}
