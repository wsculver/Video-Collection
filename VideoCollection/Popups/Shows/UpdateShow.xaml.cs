using Microsoft.Win32;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Database;
using VideoCollection.Shows;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;
using System.Threading;
using VideoCollection.Animations;
using System.Windows.Data;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for UpdateShow.xaml
    /// </summary>
    public partial class UpdateShow : Window, ScaleableWindow
    {
        private List<string> _selectedCategories;
        private int _showId;
        private Show _show;
        private List<ShowSeasonDeserialized> _seasons;
        private string _rating;
        private string _originalShowName;
        private Border _splash;
        private Action _callback;
        private string _thumbnailVisibility = "";
        private CancellationTokenSource _tokenSource;
        private ShowDeserialized _selectedShow = null;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public UpdateShow() { }

        public UpdateShow(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;

            _seasons = new List<ShowSeasonDeserialized>();
            _selectedCategories = new List<string>();
            _rating = "";
            _tokenSource = new CancellationTokenSource();

            WidthScale = 0.73;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.623;

            UpdateShowList();

            // Load categories
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> rawCategories = connection.Table<ShowCategory>().ToList().OrderBy(c => c.Name).ToList();
                List<ShowCategoryDeserialized> categories = new List<ShowCategoryDeserialized>();
                foreach (ShowCategory category in rawCategories)
                {
                    categories.Add(new ShowCategoryDeserialized(category));
                }
                icCategories.ItemsSource = categories;
            }
        }

        // Load current show list
        private void UpdateShowList()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                List<Show> rawShows = connection.Table<Show>().ToList().OrderBy(c => c.Title).ToList();
                List<ShowDeserialized> shows = new List<ShowDeserialized>();
                foreach (Show show in rawShows)
                {
                    try
                    {
                        shows.Add(new ShowDeserialized(show));
                    }
                    catch (Exception ex)
                    {
                        if (GetWindow(this).Owner != null)
                        {
                            ShowOKMessageBox("Error: " + ex.Message);
                        }
                        else
                        {
                            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                            CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                            popup.scaleWindow(parentWindow);
                            parentWindow.addChild(popup);
                            popup.Owner = parentWindow;
                            popup.ShowDialog();
                            _callback();
                        }
                    }
                }
                lvShowList.ItemsSource = shows;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvShowList.ItemsSource);
            view.Filter = ShowFilter;
            txtFilter.IsReadOnly = false;
            txtFilter.Focusable = true;
            txtFilter.IsHitTestVisible = true;
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

        // Close window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            _tokenSource.Cancel();
            _callback();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // If there are changes save them before closing
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyUpdate())
            {
                _splash.Visibility = Visibility.Collapsed;
                _callback();
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                parentWindow.removeChild(this);
                Close();
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

        // Load show info when a show is selected from the list
        private void lvShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var shows = lvShowList.SelectedItems;
            if (shows.Count > 0)
            {
                panelShowInfo.Visibility = Visibility.Visible;
                _selectedCategories = new List<string>();

                ShowDeserialized showDeserialized = (ShowDeserialized)shows[0];
                Show show = DatabaseFunctions.GetShow(showDeserialized.Id);
                txtShowFolder.Text = show.ShowFolderPath;
                txtShowName.Text = show.Title;
                _originalShowName = show.Title;
                imgThumbnail.Source = showDeserialized.Thumbnail;
                switch (show.ThumbnailVisibility)
                {
                    case "Visible":
                        btnImage.IsChecked = true;
                        break;
                    case "Collapsed":
                    default:
                        btnText.IsChecked = true;
                        break;
                }
                _thumbnailVisibility = show.ThumbnailVisibility;
                List<ShowSeason> showSeasons = JsonConvert.DeserializeObject<List<ShowSeason>>(show.Seasons);
                List<ShowSeasonDeserialized> showSeasonsDeserialized = new List<ShowSeasonDeserialized>();
                foreach (ShowSeason season in showSeasons)
                {
                    showSeasonsDeserialized.Add(new ShowSeasonDeserialized(season));
                }
                _seasons = showSeasonsDeserialized;
                _showId = show.Id;
                _show = show;
                switch(show.Rating)
                {
                    case "TV Y":
                        btnTVY.IsChecked = true;
                        break;
                    case "TV Y7":
                        btnTVY7.IsChecked = true;
                        break;
                    case "TV G":
                        btnTVG.IsChecked = true;
                        break;
                    case "TV PG":
                        btnTVPG.IsChecked = true;
                        break;
                    case "TV 14":
                        btnTV14.IsChecked = true;
                        break;
                    case "TV MA":
                        btnTVMA.IsChecked = true;
                        break;
                }
                _rating = show.Rating;
                List<ShowCategoryDeserialized> categories = new List<ShowCategoryDeserialized>();
                foreach (ShowCategoryDeserialized category in icCategories.Items)
                {
                    bool check = false;
                    if (show.Categories.Contains(category.Name))
                    {
                        check = true;
                        _selectedCategories.Add(category.Name);
                    }
                    categories.Add(new ShowCategoryDeserialized(category.Id, category.Position, category.Name, category.Shows, check));
                }
                icCategories.ItemsSource = categories;
                _selectedShow = showDeserialized;
            }
        }

        // Always save changes on apply
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyUpdate();
        }

        // Check if any movie content has changed from what was already saved
        private bool ShowContentChanged(Show show)
        {
            return (show.Title != txtShowName.Text)
                || (show.ShowFolderPath != txtShowFolder.Text)
                || (show.Thumbnail != imgThumbnail.Source.ToString())
                || (show.ThumbnailVisibility != _thumbnailVisibility)
                || (show.Seasons != _show.Seasons)
                || (show.Rating != _rating)
                || (show.Categories != JsonConvert.SerializeObject(_selectedCategories));
        }

        // Shows a custom OK message box
        private void ShowOKMessageBox(string message)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            Splash.Visibility = Visibility.Collapsed;
        }

        // Save changes
        private bool ApplyUpdate()
        {
            bool repeat = false;
            if (_selectedShow != null)
            {
                int selectedShowId = _selectedShow.Id;

                if (txtShowFolder.Text == "")
                {
                    ShowOKMessageBox("You need to select a show folder");
                    return false;
                }
                else if (txtShowName.Text == "")
                {
                    ShowOKMessageBox("You need to enter a show name");
                    return false;
                }
                else if (_thumbnailVisibility == "")
                {
                    ShowOKMessageBox("You need to select a thumbnail tile type");
                    return false;
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                    {
                        connection.CreateTable<Show>();
                        List<Show> shows = connection.Table<Show>().ToList();
                        string showName = txtShowName.Text.ToUpper();
                        Parallel.ForEach(shows, s =>
                        {
                            if (s.Title != _originalShowName && s.Title == showName)
                            {
                                repeat = true;
                            }
                        });

                        if (repeat)
                        {
                            ShowOKMessageBox("A category with that name already exists");
                        }
                        else
                        {
                            // Update the show in the Show table
                            Show show = connection.Get<Show>(_showId);
                            if (ShowContentChanged(show))
                            {
                                bool showContentChanged = ShowContentChanged(show);
                                show.Title = txtShowName.Text.ToUpper();
                                show.ShowFolderPath = txtShowFolder.Text;
                                show.Thumbnail = StaticHelpers.ImageSourceToBase64(imgThumbnail.Source);
                                show.ThumbnailVisibility = _thumbnailVisibility;
                                List<ShowSeason> seasons = new List<ShowSeason>();
                                foreach(ShowSeasonDeserialized season in _seasons)
                                {
                                    ShowSeason s = new ShowSeason(season);
                                    seasons.Add(s);
                                }
                                show.Seasons = JsonConvert.SerializeObject(seasons);
                                show.Rating = _rating;
                                show.Categories = JsonConvert.SerializeObject(_selectedCategories);
                                connection.Update(show);

                                // Update the ShowCateogry table
                                connection.CreateTable<ShowCategory>();
                                List<ShowCategory> categories = connection.Table<ShowCategory>().ToList().OrderBy(c => c.Name).ToList();
                                foreach (ShowCategory category in categories)
                                {
                                    if (!_selectedCategories.Contains(category.Name))
                                    {
                                        // Remove show from categories in the ShowCategory table
                                        DatabaseFunctions.RemoveShowFromCategory(_show.Id, category);
                                    }
                                }

                                imgThumbnail.Source.Freeze();
                                App.showThumbnails[show.Id] = imgThumbnail.Source;
                            }
                        }
                    }

                    if (!repeat)
                    {
                        string filterValue = txtFilter.Text;
                        txtFilter.Text = "";

                        UpdateShowList();
                        // Reselect the show that is being edited
                        for (int i = 0; i < lvShowList.Items.Count; i++)
                        {
                            ShowDeserialized show = (ShowDeserialized)lvShowList.Items[i];
                            if (show.Id == selectedShowId)
                            {
                                lvShowList.SelectedIndex = i;
                                break;
                            }
                        }

                        txtFilter.Text = filterValue;

                        return true;
                    }
                }
            }

            return !repeat;
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<UpdateShow>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(updateShowWindow, 500f, 800f);
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
                    _show = StaticHelpers.ParseShowVideos(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                       {
                           txtShowName.Text = _show.Title;
                           if (_show.Thumbnail != "")
                           {
                               imgThumbnail.Source = StaticHelpers.Base64ToImageSource(_show.Thumbnail);
                           }
                           loadingControl.Visibility = Visibility.Collapsed;
                       });
                }, token);
            }
        }

        // Set the show rating
        private void RatingButtonClick(object sender, RoutedEventArgs e)
        {
            _rating = (sender as RadioButton).Content.ToString();
        }

        // Delete a show from the database
        private void btnDeleteShow_Click(object sender, RoutedEventArgs e)
        {
            int showId = (int)(sender as Button).Tag;
            Show show = DatabaseFunctions.GetShow(showId);

            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + show.Title + " from the database? This only removes the show from your video collection, it does not delete any show files.", CustomMessageBox.MessageBoxType.YesNo);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                DatabaseFunctions.DeleteShow(show.Id);
                UpdateShowList();
                panelShowInfo.Visibility = Visibility.Collapsed;
            }
            Splash.Visibility = Visibility.Collapsed;
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

        private bool ShowFilter(object item)
        {
            if (String.IsNullOrEmpty(txtFilter.Text))
                return true;
            else
                return (item as ShowDeserialized).Title.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvShowList.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(lvShowList.ItemsSource).Refresh();
            }
        }
    }
}
