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
        public static HashSet<string> MovieAllCategories = new HashSet<string>() { "ALL", "ALL MOVIES" };
        public static HashSet<string> ShowAllCategories = new HashSet<string>() { "ALL", "ALL SHOWS" };

        // Remove movie with movieId from category
        public static void RemoveMovieFromCategory(int movieId, MovieCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                SortedSet<int> movieIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.MovieIds);
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
                SortedSet<int> showIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.ShowIds);
                showIds.Remove(showId);
                category.ShowIds = JsonConvert.SerializeObject(showIds);
                connection.Update(category);
            }
        }

        // Add movie object to category
        public static void AddMovieToCategory(int movieId, MovieCategory category, SQLiteConnection connection)
        {
            SortedSet<int> movieIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.MovieIds);
            movieIds.Add(movieId);
            category.MovieIds = JsonConvert.SerializeObject(movieIds);
            connection.Update(category);
        }

        // Add movie to all category
        public static void AddMovieToAllCategory(int movieId, SQLiteConnection connection)
        {
            connection.CreateTable<MovieCategory>();
            List<MovieCategory> categories = connection.Table<MovieCategory>().OrderBy(c => c.Name).ToList();
            Movie movie = connection.Get<Movie>(movieId);
            List<string> movieCategories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
            foreach (MovieCategory category in categories)
            {
                if (MovieAllCategories.Contains(category.Name.ToUpper()))
                {
                    AddMovieToCategory(movieId, category, connection);
                    movieCategories.Add(category.Name.ToUpper());
                }
            }
            movie.Categories = JsonConvert.SerializeObject(movieCategories);
            connection.Update(movie);
        }

        // Add show object to category
        public static void AddShowToCategory(int showId, ShowCategory category, SQLiteConnection connection)
        {
            SortedSet<int> showIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.ShowIds);
            showIds.Add(showId);
            category.ShowIds = JsonConvert.SerializeObject(showIds);
            connection.Update(category);
        }

        // Add movie to all category
        public static void AddShowToAllCategory(int showId, SQLiteConnection connection)
        {
            connection.CreateTable<ShowCategory>();
            List<ShowCategory> categories = connection.Table<ShowCategory>().OrderBy(c => c.Name).ToList();
            Show show = connection.Get<Show>(showId);
            List<string> showCategories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
            foreach (ShowCategory category in categories)
            {
                if (ShowAllCategories.Contains(category.Name.ToUpper()))
                {
                    AddShowToCategory(showId, category, connection);
                    showCategories.Add(category.Name.ToUpper());
                }
            }
            show.Categories = JsonConvert.SerializeObject(showCategories);
            connection.Update(show);
        }

        // Update all movies with the updated category
        public static void UpdateCategoryInMovies(string oldName, string newName, SortedSet<int> selectedMovieIds)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                List<Movie> movies = connection.Table<Movie>().ToList();
                Parallel.ForEach(movies, movie =>
                {
                    List<string> categories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                    categories.Remove(oldName);
                    if (selectedMovieIds.Contains(movie.Id) && !MovieAllCategories.Contains(newName))
                    {
                        categories.Add(newName);
                    }
                    movie.Categories = JsonConvert.SerializeObject(categories);
                    connection.Update(movie);
                });
            }
        }

        // Update all shows with the updated category
        public static void UpdateCategoryInShows(string oldName, string newName, SortedSet<int> selectedShowIds)
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
