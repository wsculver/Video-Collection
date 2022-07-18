using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using NReco.VideoInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoCollection.Movies;
using VideoCollection.Shows;
using VideoCollection.Subtitles;
using System.Text.RegularExpressions;
using SQLite;
using System.Collections.Concurrent;
using System.Threading;

namespace VideoCollection.Helpers
{
    internal static class StaticHelpers
    {
        // Get a relative path from the current application directory to a file
        // Returns a Uri
        public static Uri GetRelativePathUriFromCurrent(string fileName)
        {
            // Make sure path ends with a slash because it is a directory
            Uri currentPath = new Uri(Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri filePath = new Uri(fileName);
            Uri relativePath = currentPath.MakeRelativeUri(filePath);
            return new Uri(Uri.UnescapeDataString(relativePath.OriginalString));
        }

        // Get a relative path from the current application directory to a file
        // Returns a string
        public static string GetRelativePathStringFromCurrent(string fileName)
        {
            // Make sure path ends with a slash because it is a directory
            Uri currentPath = new Uri(Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri filePath = new Uri(fileName);
            Uri relativePath = currentPath.MakeRelativeUri(filePath);
            return Uri.UnescapeDataString(relativePath.OriginalString);
        }

        // Get the absolute path from the current directory and relative path
        public static string GetAbsolutePathStringFromRelative(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        // Create a file dialog to choose a video
        public static OpenFileDialog CreateVideoFileDialog()
        {
            OpenFileDialog filePath = new OpenFileDialog();
            filePath.DefaultExt = ".m4v";
            filePath.CheckFileExists = true;
            filePath.CheckPathExists = true;
            filePath.Multiselect = false;
            filePath.ValidateNames = true;
            filePath.Filter = "Video Files|*.m4v;*.mp4;*.MOV;*.mkv";
            return filePath;
        }

        // Create a file dialog to choose an image
        public static OpenFileDialog CreateImageFileDialog()
        {
            OpenFileDialog imagePath = new OpenFileDialog();
            imagePath.DefaultExt = ".png";
            imagePath.CheckFileExists = true;
            imagePath.CheckPathExists = true;
            imagePath.Multiselect = false;
            imagePath.ValidateNames = true;
            imagePath.Filter = "Image Files|*.png;*.jpg;*.jpeg";
            return imagePath;
        }

        // Create a file dialog to choose a folder
        public static CommonOpenFileDialog CreateFolderFileDialog()
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;
            return dlg;
        }

        // Create a file dialog to choose a subtitle file
        public static OpenFileDialog CreateSubtitleFileDialog()
        {
            OpenFileDialog filePath = new OpenFileDialog();
            filePath.DefaultExt = ".srt";
            filePath.CheckFileExists = true;
            filePath.CheckPathExists = true;
            filePath.Multiselect = false;
            filePath.ValidateNames = true;
            filePath.Filter = "Subtitle Files|*.srt";
            return filePath;
        }

        // Convert a Uri into an ImageSource
        public static ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        // Parse movie bonus video info
        private static MovieBonusVideo ParseMovieBonusVideo(string movieFolderPath, string videoFile, IEnumerable<string> subtitleFiles)
        {
            SubtitleParser subParser = new SubtitleParser();
            string bonusSection = Path.GetFileName(Path.GetDirectoryName(videoFile)).ToUpper();
            string bonusTitle = Path.GetFileNameWithoutExtension(videoFile);
            string bonusSubtitleFile = "";
            List<SubtitleSegment> bonusSubtitles = new List<SubtitleSegment>();
            var bonusSubtitleFiles = subtitleFiles
                .Where(s => s.EndsWith(bonusTitle + ".srt", StringComparison.OrdinalIgnoreCase));
            if (bonusSubtitleFiles.Any())
            {
                bonusSubtitleFile = bonusSubtitleFiles.FirstOrDefault();
                bonusSubtitles = subParser.ExtractSubtitles(bonusSubtitleFile);
            }

            var bonusImageFiles = Directory.GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(bonusTitle + ".png", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(bonusTitle + ".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(bonusTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string bonusThumbnail = "";
            if (bonusImageFiles.Any())
            {
                bonusThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(bonusImageFiles.First())));
            }
            else
            {
                ImageSource image = CreateThumbnailFromVideoFile(videoFile, TimeSpan.FromSeconds(5));
                bonusThumbnail = ImageSourceToBase64(image);
            }

            MovieBonusVideo video = new MovieBonusVideo()
            {
                Title = bonusTitle.ToUpper(),
                Thumbnail = bonusThumbnail,
                FilePath = videoFile,
                Section = bonusSection,
                Runtime = GetVideoDuration(videoFile),
                Subtitles = JsonConvert.SerializeObject(bonusSubtitles)
            };

            return video;
        }

        // Parse a movie bonus folder to format all videos in it
        public static Movie ParseMovieVideos(string movieFolderPath, CancellationToken token)
        {
            var videoFiles = Directory.GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(".MOV", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase));
            var subtitleFiles = Directory.GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".srt", StringComparison.OrdinalIgnoreCase));

            // The first video found should be the movie
            string movieFile = videoFiles.FirstOrDefault();
            string movieTitle = "";
            string movieFilePath = "";
            if (movieFile != null)
            {
                movieTitle = Path.GetFileNameWithoutExtension(movieFile);
                movieFilePath = GetRelativePathStringFromCurrent(movieFile);
            }

            // All other videos are bonus
            ConcurrentBag<MovieBonusVideo> bonusVideos = new ConcurrentBag<MovieBonusVideo>();
            ConcurrentDictionary<string, byte> bonusSectionsSet = new ConcurrentDictionary<string, byte>();
            int numVideoFiles = videoFiles.Count();
            var tasks = new List<Task<Tuple<string, string, string, List<SubtitleSegment>, string, int>>>();
            Parallel.For(1, numVideoFiles, i =>
            {
                if (token.IsCancellationRequested) return;
                string videoFile = videoFiles.ElementAt(i);
                var bonusVideo = ParseMovieBonusVideo(movieFolderPath, videoFile, subtitleFiles);
                bonusSectionsSet[bonusVideo.Section] = 0;
                bonusVideos.Add(bonusVideo);
            });
            if (token.IsCancellationRequested) return new Movie();

            List<MovieBonusSection> bonusSections = new List<MovieBonusSection>();
            foreach(KeyValuePair<string, byte> entry in bonusSectionsSet)
            {
                MovieBonusSection section = new MovieBonusSection()
                {
                    Name = entry.Key,
                    Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                };
                bonusSections.Add(section);
            }

            // Sort bonus sections so they are consistent
            bonusSections.Sort();

            // Get the thumbnail file if it exists, otherwise create one
            string thumbnailVisibility = "Collapsed";
            var imageFiles = Directory.GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(movieTitle + ".png", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(movieTitle + ".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(movieTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string movieThumbnail = "";
            if (imageFiles.Any())
            {
                movieThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(imageFiles.First())));
                thumbnailVisibility = "Visible";
            }
            else if(movieFile != null)
            {
                ImageSource image = CreateThumbnailFromVideoFile(movieFile, TimeSpan.FromSeconds(60));
                movieThumbnail = ImageSourceToBase64(image);
            }

            SubtitleParser subParser = new SubtitleParser();
            // Get the subtitle file path and parse it
            string subtitleFile = subtitleFiles
                .Where(s => s.EndsWith(movieTitle + ".srt", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            List<SubtitleSegment> subtitles = new List<SubtitleSegment>();
            if (subtitleFile != null)
            {
                subtitles = subParser.ExtractSubtitles(subtitleFile);
            }

            Movie movie = new Movie()
            {
                Title = movieTitle.ToUpper(),
                MovieFolderPath = movieFolderPath,
                Thumbnail = movieThumbnail,
                ThumbnailVisibility = thumbnailVisibility,
                MovieFilePath = movieFilePath,
                BonusSections = JsonConvert.SerializeObject(bonusSections),
                BonusVideos = JsonConvert.SerializeObject(bonusVideos),
                Categories = JsonConvert.SerializeObject(new List<string>()),
                Subtitles = JsonConvert.SerializeObject(subtitles)
            };

            return movie;
        }

        // Parse each movie folder in the root folder
        public static ConcurrentDictionary<string, Movie> ParseBulkMovies(string rootMovieFolderPath, CancellationToken token)
        {
            var movieFolders = Directory.GetDirectories(rootMovieFolderPath);

            ConcurrentDictionary<string, Movie> movies = new ConcurrentDictionary<string, Movie>();
            Parallel.ForEach(movieFolders, folder =>
            {
                var movieTitle = Path.GetFileName(folder).ToUpper();
                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();
                    List<Movie> databaseMovies = connection.Table<Movie>().ToList();
                    foreach (Movie m in databaseMovies)
                    {
                        if (m.Title == movieTitle)
                        {
                            repeat = true;
                            break;
                        }
                    }
                }

                if (!repeat)
                {
                    Movie movie = ParseMovieVideos(folder, token);
                    if (token.IsCancellationRequested) return;

                    Movie newMovie = new Movie()
                    {
                        Title = movie.Title,
                        MovieFolderPath = movie.MovieFolderPath,
                        Thumbnail = movie.Thumbnail,
                        ThumbnailVisibility = movie.ThumbnailVisibility,
                        MovieFilePath = movie.MovieFilePath,
                        Runtime = GetVideoDuration(movie.MovieFilePath),
                        BonusSections = movie.BonusSections,
                        BonusVideos = movie.BonusVideos,
                        Rating = "",
                        Categories = JsonConvert.SerializeObject(new List<string>()),
                        Subtitles = movie.Subtitles
                    };

                    movies[movie.Title] = newMovie;
                }
            });

            return movies;
        }

        // Parse show bonus video info
        private static ShowVideo ParseShowBonusVideo(string showFolderPath, string videoFile, IEnumerable<string> subtitleFiles, int seasonNumber, int episodeNumber = 0)
        {
            SubtitleParser subParser = new SubtitleParser();
            string showTitle = Path.GetFileName(showFolderPath);
            string bonusSection = Path.GetFileName(Path.GetDirectoryName(videoFile)).ToUpper();
            string bonusTitle = Path.GetFileNameWithoutExtension(videoFile);
            string bonusSubtitleFile = "";
            List<SubtitleSegment> bonusSubtitles = new List<SubtitleSegment>();
            var bonusSubtitleFiles = subtitleFiles
                .Where(s => s.EndsWith(bonusTitle + ".srt", StringComparison.OrdinalIgnoreCase));
            if (bonusSubtitleFiles.Any())
            {
                bonusSubtitleFile = bonusSubtitleFiles.FirstOrDefault();
                bonusSubtitles = subParser.ExtractSubtitles(bonusSubtitleFile);
            }

            var bonusImageFiles = Directory.GetFiles(showFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(bonusTitle + ".png", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(bonusTitle + ".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(bonusTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string bonusThumbnail = "";
            if (bonusImageFiles.Any())
            {
                bonusThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(bonusImageFiles.First())));
            }
            else
            {
                ImageSource image = CreateThumbnailFromVideoFile(videoFile, TimeSpan.FromSeconds(5));
                bonusThumbnail = ImageSourceToBase64(image);
            }

            ShowVideo video = new ShowVideo()
            {
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                Title = bonusTitle,
                ShowTitle = showTitle.ToUpper(),
                Thumbnail = bonusThumbnail,
                FilePath = GetRelativePathStringFromCurrent(videoFile),
                Commentaries = "",
                DeletedScenes = "",
                Section = bonusSection,
                Runtime = GetVideoDuration(videoFile),
                Subtitles = JsonConvert.SerializeObject(bonusSubtitles),
                NextEpisode = "",
                IsBonusVideo = true
            };

            return video;
        }

        // Parse episode info
        private static ShowVideo ParseEpisode(int seasonNumber, int episodeNumber, string seasonFolderPath, string videoFile, IEnumerable<string> subtitleFiles, ref List<ShowVideo> commentaries, ref List<ShowVideo> deletedScenes)
        {
            SubtitleParser subParser = new SubtitleParser();
            string showTitle = Path.GetFileName(Path.GetDirectoryName(seasonFolderPath));
            string episodeTitle = Path.GetFileNameWithoutExtension(videoFile).ToUpper();
            string episodeSubtitleFile = "";
            List<SubtitleSegment> episodeSubtitles = new List<SubtitleSegment>();
            var episodeSubtitleFiles = subtitleFiles
                .Where(s => s.EndsWith(episodeTitle + ".srt", StringComparison.OrdinalIgnoreCase));
            if (episodeSubtitleFiles.Any())
            {
                episodeSubtitleFile = episodeSubtitleFiles.FirstOrDefault();
                episodeSubtitles = subParser.ExtractSubtitles(episodeSubtitleFile);
            }

            var episodeImageFiles = Directory.GetFiles(seasonFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(episodeTitle + ".png", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(episodeTitle + ".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(episodeTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string episodeThumbnail = "";
            if (episodeImageFiles.Any())
            {
                episodeThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(episodeImageFiles.First())));
            }
            else
            {
                ImageSource image = CreateThumbnailFromVideoFile(videoFile, TimeSpan.FromSeconds(60));
                episodeThumbnail = ImageSourceToBase64(image);
            }

            // Link commentaries and deleted scenes if there are any
            List<ShowVideo> episodeCommentaries = null;
            List<ShowVideo> episodeDeletedScenes = null;
            List<int> removeCommentariesIndecies = new List<int>();
            int commentariesLength = commentaries.Count;
            for (int i = 0; i < commentariesLength; i++)
            {
                if (commentaries.ElementAt(i).Title.StartsWith(episodeTitle, true, null))
                {
                    if (episodeCommentaries == null)
                    {
                        episodeCommentaries = new List<ShowVideo>();
                    }
                    episodeCommentaries.Add(commentaries.ElementAt(i));
                    removeCommentariesIndecies.Add(i);
                }
            }
            // Remove already linked commentaries to improve future loops for other episodes
            foreach (int index in removeCommentariesIndecies)
            {
                commentaries.RemoveAt(index);
            }
            List<int> removeDeletedScenesIndecies = new List<int>();
            int deletedScenesLength = deletedScenes.Count;
            for (int i = 0; i < deletedScenesLength; i++)
            {
                if (deletedScenes.ElementAt(i).Title.StartsWith(episodeTitle, true, null))
                {
                    if (episodeDeletedScenes == null)
                    {
                        episodeDeletedScenes = new List<ShowVideo>();
                    }
                    episodeDeletedScenes.Add(deletedScenes.ElementAt(i));
                    removeDeletedScenesIndecies.Add(i);
                }
            }
            foreach (int index in removeDeletedScenesIndecies)
            {
                deletedScenes.RemoveAt(index);
            }

            ShowVideo episode = new ShowVideo()
            {
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                Title = episodeTitle,
                ShowTitle = showTitle.ToUpper(),
                Thumbnail = episodeThumbnail,
                FilePath = GetRelativePathStringFromCurrent(videoFile),
                Commentaries = JsonConvert.SerializeObject(episodeCommentaries),
                DeletedScenes = JsonConvert.SerializeObject(episodeDeletedScenes),
                Section = "EPISODES",
                Runtime = GetVideoDuration(videoFile),
                Subtitles = JsonConvert.SerializeObject(episodeSubtitles),
                NextEpisode = "",
                IsBonusVideo = false
            };

            return episode;
        }

        // Parse a show folder to format all videos in it
        public static Show ParseShowVideos(string showFolderPath, CancellationToken token)
        {
            string showThumbnailVideoFile = "";

            List<ShowSeason> seasons = new List<ShowSeason>();
            ShowVideo nextEpisode = new ShowVideo();
            List<List<ShowVideo>> showVideos = new List<List<ShowVideo>>();
            List<List<ShowSection>> showSections = new List<List<ShowSection>>();

            var seasonFolders = Directory.GetDirectories(showFolderPath, "*.*", SearchOption.TopDirectoryOnly);
            var showTitle = Path.GetFileName(showFolderPath);
            int numSeasons = seasonFolders.Count();
            for (int n = 0; n < numSeasons; n++)
            {
                seasons.Add(null);
                showVideos.Add(new List<ShowVideo>());
                showSections.Add(new List<ShowSection>());
            }
            Parallel.For(0, numSeasons, n =>
            {
                if (token.IsCancellationRequested) return;
                int season = n + 1;
                string seasonFolder = seasonFolders.ElementAt(n);
                Regex rx = new Regex(@"(Season)\s+(?<season>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                MatchCollection matches = rx.Matches(seasonFolder);
                Match match = matches.FirstOrDefault();
                if (match != null)
                {
                    GroupCollection groups = match.Groups;
                    bool success = int.TryParse(groups["season"].Value, out season);
                }

                List<ShowVideo> videos = new List<ShowVideo>();

                var episodeVideoFiles = Directory.GetFiles(seasonFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".MOV", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase));
                if (season == 1)
                {
                    showThumbnailVideoFile = episodeVideoFiles.FirstOrDefault();
                }
                var episodeSubtitleFiles = Directory.GetFiles(seasonFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".srt", StringComparison.OrdinalIgnoreCase));

                var subdirectories = Directory.GetDirectories(seasonFolder, "*.*", SearchOption.TopDirectoryOnly);
                IEnumerable<string> bonusVideoFiles = null;
                IEnumerable<string> bonusSubtitleFiles = null;
                foreach (var directory in subdirectories)
                {
                    if (bonusVideoFiles == null)
                    {
                        bonusVideoFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".MOV", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        bonusVideoFiles.Concat(Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".MOV", StringComparison.OrdinalIgnoreCase)
                            || s.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)));
                    }

                    if (bonusSubtitleFiles == null)
                    {
                        bonusSubtitleFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".srt", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        bonusSubtitleFiles.Concat(Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".srt", StringComparison.OrdinalIgnoreCase)));
                    }
                }

                List<ShowVideo> commentaries = new List<ShowVideo>();
                List<ShowVideo> deletedScenes = new List<ShowVideo>();

                // Bonus videos
                List<string> bonusSectionsSet = new List<string>();
                int numBonusVideoFiles = 0;
                if (bonusVideoFiles != null)
                {
                    numBonusVideoFiles = bonusVideoFiles.Count();
                }
                for (int i = 0; i < numBonusVideoFiles; i++)
                {
                    if (token.IsCancellationRequested) return;
                    string videoFile = bonusVideoFiles.ElementAt(i);
                    Regex rxBonus = new Regex(@"[S]\s*(?<season>\d+)\s*[E]\s*(?<episode>\d+)|(Episode)\s*(?<episode>\d+)|[E]\s*(?<episode>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matchesBonus = rxBonus.Matches(videoFile);
                    Match matchBonus = matchesBonus.FirstOrDefault();
                    ShowVideo video;
                    if (matchBonus != null)
                    {
                        GroupCollection groups = matchBonus.Groups;
                        int episodeNumber = 0;
                        bool success = int.TryParse(groups["episode"].Value, out episodeNumber);
                        if (success)
                        {
                            video = ParseShowBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles, season, episodeNumber);
                        }
                        else
                        {
                            video = ParseShowBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles, season);
                        }
                    }
                    else
                    {
                        video = ParseShowBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles, season);
                    }

                    bonusSectionsSet.Add(video.Section);
                    videos.Add(video);

                    // Add to commentaries and deleted scenes
                    if (video.Section.ToUpper() == "COMMENTARIES")
                    {
                        commentaries.Add(video);
                    }
                    else if (video.Section.ToUpper() == "DELETED SCENES")
                    {
                        deletedScenes.Add(video);
                    }
                }

                // Episodes
                int numEpisodeVideoFiles = episodeVideoFiles.Count();
                for (int i = 0; i < numEpisodeVideoFiles; i++)
                {
                    if (token.IsCancellationRequested) return;
                    string videoFile = episodeVideoFiles.ElementAt(i);
                    Regex rxEpisode = new Regex(@"[S]\s*(?<season>\d+)\s*[E]\s*(?<episode>\d+)|(Episode)\s*(?<episode>\d+)|[E]\s*(?<episode>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matchesEpisode = rxEpisode.Matches(videoFile);
                    Match matchEpisode = matchesEpisode.FirstOrDefault();
                    ShowVideo episode;
                    if (matchEpisode != null)
                    {
                        GroupCollection groups = matchEpisode.Groups;
                        int episodeNumber = i + 1;
                        bool success = int.TryParse(groups["episode"].Value, out episodeNumber);
                        episode = ParseEpisode(season, episodeNumber, seasonFolder, videoFile, episodeSubtitleFiles, ref commentaries, ref deletedScenes);
                    }
                    else
                    {
                        episode = ParseEpisode(season, i + 1, seasonFolder, videoFile, episodeSubtitleFiles, ref commentaries, ref deletedScenes);
                    }

                    videos.Add(episode);

                    // Set the initial next episode to episode 1
                    if (season == 1 && episode.EpisodeNumber == 1)
                    {
                        nextEpisode = episode;
                    }
                }

                List<ShowSection> sections = new List<ShowSection>();
                foreach (string bonusSection in bonusSectionsSet)
                {
                    ShowSection section = new ShowSection()
                    {
                        Name = bonusSection,
                        Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                    };
                    sections.Add(section);
                }
                sections.Sort();
                ShowSection episodeSection = new ShowSection()
                {
                    Name = "EPISODES",
                    Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                };
                sections = sections.Prepend(episodeSection).ToList();
                showSections[n] = sections;

                // Sort the videos
                showVideos[n] = videos.OrderByDescending(x => x.IsBonusVideo).ThenBy(x => x.EpisodeNumber).ToList();
            });
            if (token.IsCancellationRequested) return new Show();

            // Separate loop to make sure the next season is done for next episodes
            Parallel.For(0, numSeasons, i =>
            {
                // Set the next episode for each episode
                var seasonList = showVideos.ElementAt(i);
                var nextSeasonIndex = (i + 1) % numSeasons;
                var nextSeasonList = showVideos.ElementAt(nextSeasonIndex);
                int numVideos = seasonList.Count();
                for (int j = 0; j < numVideos - 1; j++)
                {
                    if (!seasonList.ElementAt(j).IsBonusVideo)
                    {
                        seasonList.ElementAt(j).NextEpisode = JsonConvert.SerializeObject(new Tuple<int, int>(i, j + 1));
                    }
                }
                if (!seasonList.ElementAt(numVideos - 1).IsBonusVideo)
                {
                    int videoIndex = 0;
                    int numNextVideos = nextSeasonList.Count();
                    for (int k = 0; k < numNextVideos; k++)
                    {
                        if (!nextSeasonList.ElementAt(k).IsBonusVideo && nextSeasonList.ElementAt(k).EpisodeNumber == 1)
                        {
                            videoIndex = k;
                            break;
                        }
                    }
                    seasonList.ElementAt(numVideos - 1).NextEpisode = JsonConvert.SerializeObject(new Tuple<int, int>(nextSeasonIndex, videoIndex));
                }

                ShowSeason season = new ShowSeason()
                {
                    SeasonNumber = i + 1,
                    SeasonName = Path.GetFileName(seasonFolders.ElementAt(i)).ToUpper(),
                    Sections = JsonConvert.SerializeObject(showSections.ElementAt(i)),
                    Videos = JsonConvert.SerializeObject(seasonList)
                };
                seasons[i] = season;
                if (token.IsCancellationRequested) return;
            });
            if (token.IsCancellationRequested) return new Show();

            // Get the thumbnail file if it exists, otherwise create one
           string thumbnailVisibility = "Collapsed";
            var imageFiles = Directory.GetFiles(showFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(showTitle + ".png", StringComparison.OrdinalIgnoreCase) 
                || s.EndsWith(showTitle + ".jpg", StringComparison.OrdinalIgnoreCase) 
                || s.EndsWith(showTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string showThumbnail = "";
            if (imageFiles.Any())
            {
                showThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(imageFiles.First())));
                thumbnailVisibility = "Visible";
            }
            else if (showThumbnailVideoFile != null)
            {
                ImageSource image = CreateThumbnailFromVideoFile(showThumbnailVideoFile, TimeSpan.FromSeconds(60));
                showThumbnail = ImageSourceToBase64(image);
            }

            Show show = new Show()
            {
                Title = showTitle.ToUpper(),
                ShowFolderPath = showFolderPath,
                Thumbnail = showThumbnail,
                ThumbnailVisibility = thumbnailVisibility,
                Seasons = JsonConvert.SerializeObject(seasons),
                NextEpisode = JsonConvert.SerializeObject(nextEpisode),
                Categories = JsonConvert.SerializeObject(new List<string>())
            };

            return show;
        }

        // Parse each movie folder in the root folder
        public static ConcurrentDictionary<string, Show> ParseBulkShows(string rootShowFolderPath, CancellationToken token)
        {
            var showFolders = Directory.GetDirectories(rootShowFolderPath);

            ConcurrentDictionary<string, Show> shows = new ConcurrentDictionary<string, Show>();
            Parallel.ForEach(showFolders, folder =>
            {
                var showTitle = Path.GetFileName(folder).ToUpper();
                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Show>();
                    List<Show> databaseShows = connection.Table<Show>().ToList();
                    foreach (Show s in databaseShows)
                    {
                        if (s.Title == showTitle)
                        {
                            repeat = true;
                            break;
                        }
                    }
                }

                if (!repeat)
                {
                    Show show = ParseShowVideos(folder, token);
                    if (token.IsCancellationRequested) return;

                    Show newShow = new Show()
                    {
                        Title = show.Title,
                        ShowFolderPath = show.ShowFolderPath,
                        Thumbnail = show.Thumbnail,
                        ThumbnailVisibility = show.ThumbnailVisibility,
                        Seasons = show.Seasons,
                        NextEpisode = show.NextEpisode,
                        Rating = "",
                        Categories = JsonConvert.SerializeObject(new List<string>())
                    };

                    shows[show.Title] = newShow;
                }
            });

            return shows;
        }

        // Create a thumbnail from a provided video file at the frame seconds in
        // Output thumbnail to temp file and return it as an ImageSource
        public static ImageSource CreateThumbnailFromVideoFile(string videoFile, TimeSpan time)
        {
            TimeSpan duration = GetVideoDurationTimeSpan(videoFile);
            while (time > duration)
            {
                time -= TimeSpan.FromSeconds(5);
                while (time <= TimeSpan.FromSeconds(0))
                {
                    time += TimeSpan.FromSeconds(1);
                }
            }

            string output = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

            var startInfo = new ProcessStartInfo
            {
                FileName = $"ffmpeg.exe",
                Arguments = $"-ss " + time.ToString(@"hh\:mm\:ss") + " -i \"" + videoFile + "\" -an -vf scale=200x112 -vframes 1 " + output,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();
            }

            var ms = new MemoryStream(File.ReadAllBytes(output));
            return ImageToImageSource(Image.FromStream(ms));
        }

        // Convert an Image to a base 64 string
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        // Convert an ImageSource to a base 64 string
        public static string ImageSourceToBase64(ImageSource image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image as BitmapSource));
                encoder.Save(ms);
                return ImageToBase64(Image.FromStream(ms), ImageFormat.Jpeg);
            }
        }

        // Convert a base 64 string to an ImageSource
        public static ImageSource Base64ToImageSource(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                ms.Seek(0, SeekOrigin.Begin);

                // Convert byte[] to ImageSource
                ms.Write(imageBytes, 0, imageBytes.Length);
                Image image = Image.FromStream(ms, true);

                using (MemoryStream ms2 = new MemoryStream())
                {
                    image.Save(ms2, ImageFormat.Jpeg);
                    ms2.Seek(0, SeekOrigin.Begin);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms2;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
        }

        // Convert a System.Drawing.Image to ImageSource
        public static ImageSource ImageToImageSource(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        // Get the first child of type T
        public static T GetObject<T>(DependencyObject o)
            where T : DependencyObject
        {
            if (o is T)
            { return (T)o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetObject<T>(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        // Get the first child of type T with name
        public static T GetObject<T>(DependencyObject o, string name)
            where T : DependencyObject
        {
            if (o is T && (o as FrameworkElement).Name == name)
            { return (T)o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetObject<T>(child, name);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        // Get the duration of a video file
        public static string GetVideoDuration(string filePath)
        {
            string absPath = GetAbsolutePathStringFromRelative(filePath);
            if (File.Exists(absPath))
            {
                var ffProbe = new FFProbe();
                var videoInfo = ffProbe.GetMediaInfo(absPath);
                string duration = "";
                TimeSpan videoDuration = videoInfo.Duration;
                if(videoDuration.Hours > 0)
                {
                    duration = videoDuration.ToString(@"h\:mm\:ss");
                }
                else
                {
                    duration = videoDuration.ToString(@"m\:ss");
                }
                return duration;
            }
            return "0:00:00";
        }

        // Get the duration of a video file
        public static TimeSpan GetVideoDurationTimeSpan(string filePath)
        {
            string absPath = GetAbsolutePathStringFromRelative(filePath);
            if (File.Exists(absPath))
            {
                var ffProbe = new FFProbe();
                var videoInfo = ffProbe.GetMediaInfo(absPath);
                TimeSpan videoDuration = videoInfo.Duration;
                return videoDuration;
            }
            return TimeSpan.FromSeconds(0);
        }
    }
}
