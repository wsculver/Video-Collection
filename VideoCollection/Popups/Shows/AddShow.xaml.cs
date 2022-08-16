using Microsoft.Win32;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VideoCollection.Database;
using VideoCollection.Shows;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using System.IO;
using VideoCollection.Animations;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;
using System.Threading;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for AddShow.xaml
    /// </summary>
    public partial class AddShow : Window, ScaleableWindow
    {
        private List<string> _selectedCategories;
        private Show _show;
        private string _rating = "";
        private Border _splash;
        private Action _callback;
        private string _thumbnailVisibility = "";
        private CancellationTokenSource _tokenSource;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddShow() { }

        public AddShow(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;
            _selectedCategories = new List<string>();
            _show = new Show();
            _tokenSource = new CancellationTokenSource();

            WidthScale = 0.43;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.058;

            // Load categories to display
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> rawCategories = connection.Table<ShowCategory>().OrderBy(c => c.Name).ToList();
                SortedSet<ShowCategoryDeserialized> categories = new SortedSet<ShowCategoryDeserialized>();
                foreach (ShowCategory category in rawCategories)
                {
                    categories.Add(new ShowCategoryDeserialized(category));
                }
                icCategories.ItemsSource = categories;
            }
        }

        // Select a category
        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Add(checkBox.Content.ToString());
        }

        // Unselect a category
        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Remove(checkBox.Content.ToString());
        }

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            _tokenSource.Cancel();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtShowFolder.Text == "")
            {
                Messages.ShowOKMessageBox("You need to select a show folder", ref Splash);
            }
            else if (txtShowName.Text == "")
            {
                Messages.ShowOKMessageBox("You need to enter a show name", ref Splash);
            }
            else if (_thumbnailVisibility == "")
            {
                Messages.ShowOKMessageBox("You need to select a thumbnail tile type", ref Splash);
            }
            else
            {
                string thumbnail = "";
                if (imgThumbnail.Source == null)
                {
                    var episodeVideoFiles = Directory.GetFiles(txtShowFolder.Text, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".m4v") || s.EndsWith(".mp4") || s.EndsWith(".MOV") || s.EndsWith(".mkv"));
                    string video = episodeVideoFiles.FirstOrDefault();
                    if (video != null)
                    {
                        ImageSource image = StaticHelpers.CreateThumbnailFromVideoFile(StaticHelpers.GetAbsolutePathStringFromRelative(video), TimeSpan.FromSeconds(60));
                        thumbnail = StaticHelpers.ImageSourceToBase64(image);
                    } 
                    else
                    {
                        Messages.ShowOKMessageBox("Unable to create a thumbnail. Please select one manually.", ref Splash);
                        return;
                    }
                }
                else
                {
                    thumbnail = StaticHelpers.ImageSourceToBase64(imgThumbnail.Source);
                }

                Show show = new Show()
                {
                    Title = txtShowName.Text.ToUpper(),
                    ShowFolderPath = txtShowFolder.Text,
                    Thumbnail = thumbnail,
                    ThumbnailVisibility = _thumbnailVisibility,
                    Seasons = _show.Seasons,
                    NextEpisode = _show.NextEpisode,
                    Rating = _rating,
                    Categories = JsonConvert.SerializeObject(_selectedCategories)
                };

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Show>();
                    List<Show> shows = connection.Table<Show>().ToList();
                    foreach (Show m in shows)
                    {
                        if (m.Title == txtShowName.Text.ToUpper())
                        {
                            repeat = true;
                            break;
                        }
                    }

                    if (repeat)
                    {
                        Messages.ShowOKMessageBox("A show with that name already exists", ref Splash);
                    }
                    else
                    {
                        connection.Insert(show);

                        connection.CreateTable<ShowCategory>();
                        List<ShowCategory> categories = connection.Table<ShowCategory>().OrderBy(c => c.Name).ToList();
                        foreach (ShowCategory category in categories)
                        {
                            if (_selectedCategories.Contains(category.Name))
                            {
                                DatabaseFunctions.AddShowToCategory(show.Id, category, connection);
                            }
                        }
                        DatabaseFunctions.AddShowToAllCategory(show.Id, connection);

                        imgThumbnail.Source.Freeze();
                        App.showThumbnails[show.Id] = imgThumbnail.Source;
                    }
                }

                if (!repeat)
                {
                    _splash.Visibility = Visibility.Collapsed;
                    _callback();
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    parentWindow.removeChild(this);
                    Close();
                }
            }
        }

        // Choose image file
        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog imagePath = StaticHelpers.CreateImageFileDialog();
            if (imagePath.ShowDialog() == true)
            {
                imgThumbnail.Source = StaticHelpers.BitmapFromUri(StaticHelpers.GetRelativePathUriFromCurrent(imagePath.FileName));
            }
        }

        // Choose the whole show folder
        private void btnChooseShowFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = StaticHelpers.CreateFolderFileDialog();
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtShowFolder.Text = StaticHelpers.GetRelativePathStringFromCurrent(dlg.FileName);
                loadingControl.Content = new LoadingSpinner();
                loadingControl.Visibility = Visibility.Visible;
                var token = _tokenSource.Token;
                Task.Run(() =>
                {
                    var result = StaticHelpers.ParseShowVideos(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        if (result.IsSuccess)
                        {
                            _show = result.Value;
                            txtShowName.Text = _show.Title;
                            if (_show.Thumbnail != "")
                            {
                                imgThumbnail.Source = StaticHelpers.Base64ToImageSource(_show.Thumbnail);
                            }

                            panelShowFields.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtShowFolder.Text = "";
                            Messages.Error(result.Error, ref Splash, "Parse");
                        }
                        loadingControl.Visibility = Visibility.Collapsed;
                    });
                }, token);
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddShow>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addShowWindow, 500f, 500f);
        }

        // Set the show rating
        private void RatingButtonClick(object sender, RoutedEventArgs e)
        {
            _rating = (sender as RadioButton).Content.ToString();
        }

        // Set the thumbnail tile visibility
        private void ThumbnailTileButtonClick(object sender, RoutedEventArgs e)
        {
            string option = (sender as RadioButton).Content.ToString();
            switch (option)
            {
                case "Image":
                    _thumbnailVisibility = "Visible";
                    break;
                case "Text":
                default:
                    _thumbnailVisibility = "Collapsed";
                    break;
            }
        }

        public void scaleWindow(Window parent)
        {
            Width = parent.ActualWidth * WidthScale;
            Height = Width * HeightToWidthRatio;
            if (Height > parent.ActualHeight * HeightScale)
            {
                Height = parent.ActualHeight * HeightScale;
                Width = Height / HeightToWidthRatio;
            }

            Left = parent.Left + (parent.Width - ActualWidth) / 2;
            Top = parent.Top + (parent.Height - ActualHeight) / 2;
        }
    }
}
