using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoCollection.Movies;
using VideoCollection.Shows;

namespace VideoCollection.Database
{
    internal static class DatabaseFunctions
    {
        // Remove movie with movieId from category
        public static void RemoveMovieFromCategory(string movieId, MovieCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> deserializedMovies = JsonConvert.DeserializeObject<List<Movie>>(category.Movies);
                for (int i = 0; i < deserializedMovies.Count; i++)
                {
                    if (deserializedMovies[i].Id.ToString() == movieId)
                    {
                        deserializedMovies.RemoveAt(i);
                    }
                }
                category.Movies = JsonConvert.SerializeObject(deserializedMovies);
                connection.Update(category);
            }
        }

        // Remove movie with showId from category
        public static void RemoveShowFromCategory(string showId, ShowCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Show> deserializedShows = JsonConvert.DeserializeObject<List<Show>>(category.Shows);
                for (int i = 0; i < deserializedShows.Count; i++)
                {
                    if (deserializedShows[i].Id.ToString() == showId)
                    {
                        deserializedShows.RemoveAt(i);
                    }
                }
                category.Shows = JsonConvert.SerializeObject(deserializedShows);
                connection.Update(category);
            }
        }

        // Add movie object to category
        public static void AddMovieToCategory(Movie movie, MovieCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> movies = JsonConvert.DeserializeObject<List<Movie>>(category.Movies);
                movies.Add(movie);
                movies.Sort();
                category.Movies = JsonConvert.SerializeObject(movies);
                connection.Update(category);
            }
        }

        // Add show object to category
        public static void AddShowToCategory(Show show, ShowCategory category)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Show> shows = JsonConvert.DeserializeObject<List<Show>>(category.Shows);
                shows.Add(show);
                shows.Sort();
                category.Shows = JsonConvert.SerializeObject(shows);
                connection.Update(category);
            }
        }

        // Remove old movie from category based on its Id and add the new movie object to the category
        public static void UpdateMovieInCategory(Movie movie, MovieCategory category)
        {
            RemoveMovieFromCategory(movie.Id.ToString(), category);
            AddMovieToCategory(movie, category);
        }

        // Remove old show from category based on its Id and add the new show object to the category
        public static void UpdateShowInCategory(Show show, ShowCategory category)
        {
            RemoveShowFromCategory(show.Id.ToString(), category);
            AddShowToCategory(show, category);
        }

        // Update all movies with the updated category
        public static void UpdateCategoryInMovies(string oldName, string newName, List<Movie> selectedMovies)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Movie> movies = (connection.Table<Movie>().ToList()).ToList();
                foreach(Movie movie in movies)
                {
                    List<string> categories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                    categories.Remove(oldName);
                    foreach (Movie selectedMovie in selectedMovies)
                    {
                        if (selectedMovie.Id == movie.Id)
                        {
                            categories.Add(newName);
                        }
                    }
                    movie.Categories = JsonConvert.SerializeObject(categories);
                    connection.Update(movie);
                }
            }
        }

        // Update all shows with the updated category
        public static void UpdateCategoryInShows(string oldName, string newName, List<Show> selectedShows)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<Show> shows = (connection.Table<Show>().ToList()).ToList();
                foreach (Show show in shows)
                {
                    List<string> categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                    categories.Remove(oldName);
                    foreach (Show selectedShow in selectedShows)
                    {
                        if (selectedShow.Id == show.Id)
                        {
                            categories.Add(newName);
                        }
                    }
                    show.Categories = JsonConvert.SerializeObject(categories);
                    connection.Update(show);
                }
            }
        }

        // Delete a movie from the database
        public static void DeleteMovie(Movie movie)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<MovieCategory> movieCategories = connection.Table<MovieCategory>().ToList();
                foreach (MovieCategory category in movieCategories)
                {
                    List<Movie> movies = JsonConvert.DeserializeObject<List<Movie>>(category.Movies);
                    foreach (Movie m in movies)
                    {
                        if (m.Id == movie.Id)
                        {
                            movies.Remove(m);
                            break;
                        }
                    }
                    category.Movies = JsonConvert.SerializeObject(movies);
                    connection.Update(category);
                }
                connection.Delete<Movie>(movie.Id);
            }
        }

        // Delete a show from the database
        public static void DeleteShow(Show show)
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                List<ShowCategory> showCategories = connection.Table<ShowCategory>().ToList();
                foreach (ShowCategory category in showCategories)
                {
                    List<Show> shows = JsonConvert.DeserializeObject<List<Show>>(category.Shows);
                    foreach (Show m in shows)
                    {
                        if (m.Id == show.Id)
                        {
                            shows.Remove(m);
                            break;
                        }
                    }
                    category.Shows = JsonConvert.SerializeObject(shows);
                    connection.Update(category);
                }
                connection.Delete<Show>(show.Id);
            }
        }
    }
}
