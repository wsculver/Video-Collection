using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Newtonsoft.Json;
using NReco.VideoInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoCollection.Movies;
using VideoCollection.Shows;
using VideoCollection.Subtitles;
using System.Text.RegularExpressions;

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

        // Parse bonus video info
        private static async Task<Tuple<string, string, string, List<SubtitleSegment>, string, int>> ParseBonusVideo(string movieFolderPath, string videoFile, IEnumerable<string> subtitleFiles, int episodeNumber = 0)
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
                bonusThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(GetRelativePathStringFromCurrent(bonusImageFiles.First()))));
            }
            else
            {
                await Task.Run(() =>
                {
                    ImageSource image = CreateThumbnailFromVideoFile(videoFile, TimeSpan.FromSeconds(5));
                    bonusThumbnail = ImageSourceToBase64(image);
                });
            }

            return Tuple.Create(bonusSection, bonusTitle, bonusThumbnail, bonusSubtitles, videoFile, episodeNumber);
        }

        // Parse a movie bonus folder to format all videos in it
        public static async Task<Movie> ParseMovieVideos(string movieFolderPath)
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
            List<MovieBonusVideo> bonusVideos = new List<MovieBonusVideo>();
            HashSet<string> bonusSectionsSet = new HashSet<string>();
            int numVideoFiles = videoFiles.Count();
            var tasks = new List<Task<Tuple<string, string, string, List<SubtitleSegment>, string, int>>>();
            for(int i = 1; i < numVideoFiles; i++)
            {
                string videoFile = videoFiles.ElementAt(i);
                tasks.Add(ParseBonusVideo(movieFolderPath, videoFile, subtitleFiles));
            }
            foreach (var task in await Task.WhenAll(tasks))
            {
                bonusSectionsSet.Add(task.Item1);
                MovieBonusVideo video = new MovieBonusVideo()
                {
                    Title = task.Item2.ToUpper(),
                    Thumbnail = task.Item3,
                    FilePath = task.Item5,
                    Section = task.Item1,
                    Runtime = GetVideoDuration(task.Item5),
                    Subtitles = JsonConvert.SerializeObject(task.Item4)
                };
                bonusVideos.Add(video);
            }

            List<MovieBonusSection> bonusSections = new List<MovieBonusSection>();
            foreach(string sectionName in bonusSectionsSet)
            {
                MovieBonusSection section = new MovieBonusSection()
                {
                    Name = sectionName,
                    Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                };
                bonusSections.Add(section);
            }

            // Get the thumbnail file if it exists, otherwise create one
            var imageFiles = Directory.GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(movieTitle + ".png", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(movieTitle + ".jpg", StringComparison.OrdinalIgnoreCase)
                || s.EndsWith(movieTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string movieThumbnail = "";
            if (imageFiles.Any())
            {
                movieThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(GetRelativePathStringFromCurrent(imageFiles.First()))));
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
                Thumbnail = movieThumbnail,
                MovieFilePath = movieFilePath,
                BonusSections = JsonConvert.SerializeObject(bonusSections),
                BonusVideos = JsonConvert.SerializeObject(bonusVideos),
                Categories = "",
                Subtitles = JsonConvert.SerializeObject(subtitles),
                IsChecked = false
            };

            return movie;
        }

        // Parse episode info
        private static async Task<Tuple<int, string, string, List<SubtitleSegment>, string>> ParseEpisode(int episodeNumber, string seasonFolderPath, string videoFile, IEnumerable<string> subtitleFiles)
        {
            SubtitleParser subParser = new SubtitleParser();
            string episodeTitle = Path.GetFileNameWithoutExtension(videoFile);
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
                episodeThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(GetRelativePathStringFromCurrent(episodeImageFiles.First()))));
            }
            else
            {
                await Task.Run(() =>
                {
                    ImageSource image = CreateThumbnailFromVideoFile(videoFile, TimeSpan.FromSeconds(60));
                    episodeThumbnail = ImageSourceToBase64(image);
                });
            }

            return Tuple.Create(episodeNumber, episodeTitle, episodeThumbnail, episodeSubtitles, videoFile);
        }

        // Parse a show folder to format all videos in it
        public static async Task<Show> ParseShowVideos(string showFolderPath)
        {
            string showThumbnailVideoFile = "";

            List<ShowSeason> seasons = new List<ShowSeason>();
            ShowVideo nextEpisode = new ShowVideo();
            List<List<ShowVideo>> showVideos = new List<List<ShowVideo>>();
            List<List<ShowSection>> showSections = new List<List<ShowSection>>();

            var seasonFolders = Directory.GetDirectories(showFolderPath, "*.*", SearchOption.TopDirectoryOnly);
            var showTitle = Path.GetFileName(Path.GetDirectoryName(seasonFolders.First()));
            int numSeasons = seasonFolders.Count();
            for (int n = 0; n < numSeasons; n++)
            {
                showVideos.Add(new List<ShowVideo>());
            }
            for (int n = 0; n < numSeasons; n++)
            {
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
                foreach(var directory in subdirectories)
                {
                    if(bonusVideoFiles == null)
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

                    if(bonusSubtitleFiles == null)
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

                // Episodes
                var episodeTasks = new List<Task<Tuple<int, string, string, List<SubtitleSegment>, string>>>();
                int numEpisodeVideoFiles = episodeVideoFiles.Count();
                for (int i = 0; i < numEpisodeVideoFiles; i++)
                {
                    string videoFile = episodeVideoFiles.ElementAt(i);
                    Regex rxEpisode = new Regex(@"[S](?<season>\d+)\s+[E](?<episode>\d+)|(Episode)\s+(?<episode>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matchesEpisode = rxEpisode.Matches(videoFile);
                    Match matchEpisode = matchesEpisode.FirstOrDefault();
                    if (matchEpisode != null)
                    {
                        GroupCollection groups = matchEpisode.Groups;
                        int episodeNumber = i + 1;
                        bool success = int.TryParse(groups["episode"].Value, out episodeNumber);
                        episodeTasks.Add(ParseEpisode(episodeNumber, showFolderPath, videoFile, episodeSubtitleFiles));
                    }
                    else
                    {
                        episodeTasks.Add(ParseEpisode(i + 1, showFolderPath, videoFile, episodeSubtitleFiles));
                    }
                }

                // Bonus videos
                HashSet<string> bonusSectionsSet = new HashSet<string>();
                int numBonusVideoFiles = 0;
                if (bonusVideoFiles != null)
                {
                    numBonusVideoFiles = bonusVideoFiles.Count();
                }
                var bonusTasks = new List<Task<Tuple<string, string, string, List<SubtitleSegment>, string, int>>>();
                for (int i = 0; i < numBonusVideoFiles; i++)
                {
                    string videoFile = bonusVideoFiles.ElementAt(i);
                    Regex rxBonus = new Regex(@"[S](?<season>\d+)\s+[E](?<episode>\d+)|(Episode)\s+(?<episode>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matchesBonus = rxBonus.Matches(videoFile);
                    Match matchBonus = matchesBonus.FirstOrDefault();
                    if (matchBonus != null)
                    {
                        GroupCollection groups = matchBonus.Groups;
                        int episodeNumber = 0;
                        bool success = int.TryParse(groups["episode"].Value, out episodeNumber);
                        if(success)
                        {
                            bonusTasks.Add(ParseBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles, episodeNumber));
                        }
                        else
                        {
                            bonusTasks.Add(ParseBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles));
                        }
                    }
                    else
                    {
                        bonusTasks.Add(ParseBonusVideo(showFolderPath, videoFile, bonusSubtitleFiles));
                    }
                }

                // Wait for episode and bonus tasks to finish
                foreach (var task in await Task.WhenAll(bonusTasks))
                {
                    bonusSectionsSet.Add(task.Item1);
                    ShowVideo video = new ShowVideo()
                    {
                        SeasonNumber = season,
                        EpisodeNumber = task.Item6,
                        Title = task.Item2.ToUpper(),
                        ShowTitle = showTitle.ToUpper(),
                        Thumbnail = task.Item3,
                        FilePath = task.Item5,
                        Commentaries = "",
                        DeletedScenes = "",
                        Section = task.Item1,
                        Runtime = GetVideoDuration(task.Item5),
                        Subtitles = JsonConvert.SerializeObject(task.Item4),
                        NextEpisode = ""
                    };
                    videos.Add(video);

                    // Add to commentaries and deleted scenes
                    if (task.Item1.ToUpper() == "COMMENTARIES")
                    {
                        commentaries.Add(video);
                    }
                    else if (task.Item1.ToUpper() == "DELETED SCENES")
                    {
                        deletedScenes.Add(video);
                    }
                }
                List<ShowVideo> episodes = new List<ShowVideo>();
                foreach (var task in await Task.WhenAll(episodeTasks))
                {
                    // Link commentaries and deleted scenes if there are any
                    List<ShowVideo> episodeCommentaries = null;
                    ShowVideo episodeDeletedScenes = null;
                    foreach (ShowVideo com in commentaries)
                    {
                        if (com.Title.StartsWith(task.Item2, true, null))
                        {
                            if (episodeCommentaries == null)
                            {
                                episodeCommentaries = new List<ShowVideo>();
                            }
                            episodeCommentaries.Add(com);
                        }
                    }
                    foreach (ShowVideo del in deletedScenes)
                    {
                        if (del.Title.StartsWith(task.Item2, true, null))
                        {
                            episodeDeletedScenes = del;
                            break;
                        }
                    }

                    ShowVideo episode = new ShowVideo()
                    {
                        SeasonNumber = season,
                        EpisodeNumber = task.Item1,
                        Title = task.Item2.ToUpper(),
                        ShowTitle = showTitle.ToUpper(),
                        Thumbnail = task.Item3,
                        FilePath = task.Item5,
                        Commentaries = JsonConvert.SerializeObject(episodeCommentaries),
                        DeletedScenes = JsonConvert.SerializeObject(episodeDeletedScenes),
                        Section = "EPISODES",
                        Runtime = GetVideoDuration(task.Item5),
                        Subtitles = JsonConvert.SerializeObject(task.Item4),
                        NextEpisode = ""
                    };
                    videos.Add(episode);

                    // Set the initial next episode to episode 1
                    if (season == 1 && task.Item1 == 1)
                    {
                        nextEpisode = episode;
                    }
                }

                List<ShowSection> sections = new List<ShowSection>();
                ShowSection episodeSection = new ShowSection()
                {
                    Name = "EPISODES",
                    Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                };
                sections.Add(episodeSection);
                foreach (string sectionName in bonusSectionsSet)
                {
                    ShowSection section = new ShowSection()
                    {
                        Name = sectionName,
                        Background = JsonConvert.SerializeObject(System.Windows.Media.Color.FromArgb(0, 0, 0, 0))
                    };
                    sections.Add(section);
                }
                showSections.Add(sections);

                // Sort the videos
                showVideos[season - 1] = videos.OrderBy(x => x.IsBonusVideo).ThenBy(x => x.EpisodeNumber).ToList();
            }

            int numShowVideos = showVideos.Count();
            for (int i = 0; i < numShowVideos; i++)
            {
                // Set the next episode for each episode
                var seasonList = showVideos.ElementAt(i);
                var nextSeasonIndex = (i < numShowVideos - 1) ? (i + 1) : 0;
                var nextSeasonList = showVideos.ElementAt(nextSeasonIndex);
                int numVideos = seasonList.Count();
                for (int j = 0; j < numVideos - 1; j++)
                {
                    if (seasonList.ElementAt(j).EpisodeNumber > 0)
                    {
                        seasonList.ElementAt(j).NextEpisode = JsonConvert.SerializeObject(new Tuple<int, int>(i, j + 1));
                    }
                }
                if (seasonList.ElementAt(numVideos - 1).EpisodeNumber > 0)
                {
                    int videoIndex = 0;
                    int numNextVideos = nextSeasonList.Count();
                    for(int k = 0; k < numNextVideos; k++)
                    {
                        if(nextSeasonList.ElementAt(k).EpisodeNumber == 1)
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
                seasons.Add(season);
            }

            // Get the thumbnail file if it exists, otherwise create one
            var imageFiles = Directory.GetFiles(showFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(showTitle + ".png", StringComparison.OrdinalIgnoreCase) 
                || s.EndsWith(showTitle + ".jpg", StringComparison.OrdinalIgnoreCase) 
                || s.EndsWith(showTitle + ".jpeg", StringComparison.OrdinalIgnoreCase));
            string showThumbnail = "";
            if (imageFiles.Any())
            {
                showThumbnail = ImageSourceToBase64(BitmapFromUri(new Uri(GetRelativePathStringFromCurrent(imageFiles.First()))));
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
                Seasons = JsonConvert.SerializeObject(seasons),
                NextEpisode = JsonConvert.SerializeObject(nextEpisode),
                Categories = "",
                IsChecked = false
            };

            return show;
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
            if (File.Exists(filePath))
            {
                var ffProbe = new FFProbe();
                var videoInfo = ffProbe.GetMediaInfo(filePath);
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
            if (File.Exists(filePath))
            {
                var ffProbe = new FFProbe();
                var videoInfo = ffProbe.GetMediaInfo(filePath);
                TimeSpan videoDuration = videoInfo.Duration;
                return videoDuration;
            }
            return TimeSpan.FromSeconds(0);
        }
    }
}
