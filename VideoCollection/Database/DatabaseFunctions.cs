using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoCollection.Movies;
using VideoCollection.Shows;

namespace VideoCollection.Database
{
    internal static class DatabaseFunctions
    {
        // Remove movie with movieId from category
        public static void RemoveMovieFromCategory(int movieId, MovieCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<int> movieIds = JsonConvert.DeserializeObject<List<int>>(category.MovieIds);
                movieIds.Remove(movieId);
                category.MovieIds = JsonConvert.SerializeObject(movieIds);
                connection.Update(category);
            }
        }

        // Remove movie with showId from category
        public static void RemoveShowFromCategory(int showId, ShowCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<int> showIds = JsonConvert.DeserializeObject<List<int>>(category.ShowIds);
                showIds.Remove(showId);
                category.ShowIds = JsonConvert.SerializeObject(showIds);
                connection.Update(category);
            }
        }

        // Add movie object to category
        public static void AddMovieToCategory(int movieId, MovieCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<int> movieIds = JsonConvert.DeserializeObject<List<int>>(category.MovieIds);
                movieIds.Add(movieId);
                movieIds.Sort();
                category.MovieIds = JsonConvert.SerializeObject(movieIds);
                connection.Update(category);
            }
        }

        // Add show object to category
        public static void AddShowToCategory(int showId, ShowCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<int> showIds = JsonConvert.DeserializeObject<List<int>>(category.ShowIds);
                showIds.Add(showId);
                showIds.Sort();
                category.ShowIds = JsonConvert.SerializeObject(showIds);
                connection.Update(category);
            }
        }

        // Update all movies with the updated category
        public static void UpdateCategoryInMovies(string oldName, string newName, List<int> selectedMovieIds)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                List<Movie> movies = connection.Table<Movie>().ToList();
                Parallel.ForEach(movies, movie =>
                {
                    List<string> categories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                    categories.Remove(oldName);
                    if (selectedMovieIds.Contains(movie.Id))
                    {
                        categories.Add(newName);
                    }
                    movie.Categories = JsonConvert.SerializeObject(categories);
                    connection.Update(movie);
                });
            }
        }

        // Update all shows with the updated category
        public static void UpdateCategoryInShows(string oldName, string newName, List<int> selectedShowIds)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                List<Show> shows = connection.Table<Show>().ToList();
                Parallel.ForEach(shows, show =>
                {
                    List<string> categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                    categories.Remove(oldName);
                    if (selectedShowIds.Contains(show.Id))
                    {
                        categories.Add(newName);
                    }
                    show.Categories = JsonConvert.SerializeObject(categories);
                    connection.Update(show);
                });
            }
        }

        // Delete a movie from the database
        public static void DeleteMovie(int movieId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> movieCategories = connection.Table<MovieCategory>().ToList();
                foreach (MovieCategory category in movieCategories)
                {
                    RemoveMovieFromCategory(movieId, category);
                }
                connection.Delete<Movie>(movieId);
                App.movieThumbnails.Remove(movieId, out var val);
            }
        }

        // Delete a show from the database
        public static void DeleteShow(int showId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> showCategories = connection.Table<ShowCategory>().ToList();
                foreach (ShowCategory category in showCategories)
                {
                    RemoveShowFromCategory(showId, category);
                }
                connection.Delete<Show>(showId);
                App.showThumbnails.Remove(showId, out var val);
            }
        }

        public static Movie GetMovie(int movieId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                return connection.Get<Movie>(movieId);
            }
        }

        public static Show GetShow(int showId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                return connection.Get<Show>(showId);
            }
        }

        public static Show GetShow(string title)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                return connection.Table<Show>().FirstOrDefault(x => x.Title == title);
            }
        }

        public static MovieCategory GetMovieCategory(int categoryId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                return connection.Get<MovieCategory>(categoryId);
            }
        }

        public static ShowCategory GetShowCategory(int categoryId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                return connection.Get<ShowCategory>(categoryId);
            }
        }
    }
}
