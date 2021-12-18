using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using VideoCollection.Movies;

namespace VideoCollection.Database
{
    internal static class DatabaseFunctions
    {
        public static void RemoveMovieFromCategory(string movieId, MovieCategory category)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> deserializedMovies = jss.Deserialize<List<Movie>>(category.Movies);
                for (int i = 0; i < deserializedMovies.Count; i++)
                {
                    if (deserializedMovies[i].Id.ToString() == movieId)
                    {
                        deserializedMovies.RemoveAt(i);
                    }
                }
                category.Movies = jss.Serialize(deserializedMovies);
                connection.Update(category);
            }
        }

        public static void AddMovieToCategory(Movie movie, MovieCategory category)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> deserializedMovies = jss.Deserialize<List<Movie>>(category.Movies);
                deserializedMovies.Add(movie);
                category.Movies = jss.Serialize(deserializedMovies);
                connection.Update(category);
            }
        }
    }
}
