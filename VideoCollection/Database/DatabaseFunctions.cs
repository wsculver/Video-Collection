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
        // Remove movie with movieId from category
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

        // Add movie object to category
        public static void AddMovieToCategory(Movie movie, MovieCategory category)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> movies = jss.Deserialize<List<Movie>>(category.Movies);
                movies.Add(movie);
                movies.Sort();
                category.Movies = jss.Serialize(movies);
                connection.Update(category);
            }
        }

        // Remove old movie from category based on its Id and add the new movie object to the category
        public static void UpdateMovieInCategory(Movie movie, MovieCategory category)
        {
            RemoveMovieFromCategory(movie.Id.ToString(), category);
            AddMovieToCategory(movie, category);
        }

        // Update all movies with the updated category
        public static void UpdateCategoryInMovies(string oldName, string newName, List<Movie> selectedMovies)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> movies = (connection.Table<Movie>().ToList()).ToList();
                foreach(Movie movie in movies)
                {
                    List<string> categories = jss.Deserialize<List<string>>(movie.Categories);
                    categories.Remove(oldName);
                    foreach (Movie selectedMovie in selectedMovies)
                    {
                        if (selectedMovie.Id == movie.Id)
                        {
                            categories.Add(newName);
                        }
                    }
                    movie.Categories = jss.Serialize(categories);
                    connection.Update(movie);
                }
            }
        }

        // Delete a movie from the database
        public static void DeleteMovie(Movie movie)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<MovieCategory> movieCategories = connection.Table<MovieCategory>().ToList();
                foreach (MovieCategory category in movieCategories)
                {
                    List<Movie> movies = jss.Deserialize<List<Movie>>(category.Movies);
                    foreach (Movie m in movies)
                    {
                        if (m.Id == movie.Id)
                        {
                            movies.Remove(m);
                            break;
                        }
                    }
                    category.Movies = jss.Serialize(movies);
                    connection.Update(category);
                }
                connection.Delete<Movie>(movie.Id);
            }
        }
    }
}
