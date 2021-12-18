using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VideoCollection.Movies;
using VideoCollection.Popups;
using VideoCollection.ViewModels;

namespace VideoCollection.Commands
{
    internal class MovieClickCommand : ICommand
    {
        private MoviesViewModel viewModel;

        public MovieClickCommand(MoviesViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                List<Movie> result = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + parameter.ToString());
                VideoPlayer popup = new VideoPlayer();
                popup.meVideoPlayer.Source = new Uri(result[0].MovieFilePath); 
                popup.Show();
            }
        }
    }
}
