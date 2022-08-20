using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Shows;
using VideoCollection.CustomTypes;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for ViewAll.xaml
    /// </summary>
    public partial class ShowViewAll : Window, ScaleableWindow
    {
        private int _categoryId;
        private bool _categoryChanged = false;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public ShowViewAll() { }

        public ShowViewAll(int id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _categoryId = id;
            _splash = splash;
            _callback = callback;

            WidthScale = 0.93;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.489;

            UpdateCategory();
        }

        // Refresh to show current database data
        private void UpdateCategory()
        {
            ShowCategory category = DatabaseFunctions.GetShowCategory(_categoryId);
            ShowCategoryDeserialized categoryDeserialized = new ShowCategoryDeserialized(category);
            labelCategory.Content = categoryDeserialized.Name;
            icVideos.ItemsSource = categoryDeserialized.Shows;
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<ShowViewAll>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(showViewAllWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            if (_categoryChanged)
            {
                _callback();
            }
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Show the show details when a show thumbnail is clicked
        private void showTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                ShowDetails popup = new ShowDetails((int)(sender as Grid).Tag, ref Splash, () => 
                {
                    this.Show();
                    this.Activate();
                });
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                popup.Show();
                popup.Activate();
                this.Hide();
            }
        }

        // Show info icon when hovering a thumbnail
        private void showTile_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>(sender as Grid).Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>(sender as Grid, "showSplash").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>(sender as Grid, "iconPlayShow").Visibility = Visibility.Visible;
        }

        // Hide info icon when not hovering a thumbnail
        private void showTile_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>(sender as Grid).Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>(sender as Grid, "showSplash").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>(sender as Grid, "iconPlayShow").Visibility = Visibility.Collapsed;
        }

        // Play the show directly
        private void btnPlayShow_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as Button).Tag;
            Show show = DatabaseFunctions.GetShow(id);
            try
            {
                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(show);
                        App.videoPlayer = popup;
                        popup.Width = parentWindow.ActualWidth;
                        popup.Height = parentWindow.ActualHeight;
                        popup.Owner = parentWindow;
                        popup.Left = popup.LeftMultiplier = parentWindow.Left;
                        popup.Top = popup.TopMultiplier = parentWindow.Top;
                        popup.Show();
                        popup.Activate();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox popup = new CustomMessageBox(ex.Message, CustomMessageBox.MessageBoxType.OK);
                        popup.scaleWindow(parentWindow);
                        parentWindow.addChild(popup);
                        popup.Owner = parentWindow;
                        popup.ShowDialog();
                    }
                }
                else
                {
                    App.videoPlayer.updateVideo(show);
                }
            }
            catch (Exception ex)
            {
                Messages.Error(ex.Message, ref Splash);
                _callback();
            }
        }

        // Show the show details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            ShowDetails popup = new ShowDetails((int)(sender as Button).Tag, ref Splash, () => 
            {
                this.Show();
                this.Activate();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
            this.Hide();
        }

        // Delete the show from the database
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
                _categoryChanged = true;
                DatabaseFunctions.DeleteShow(showId);
                UpdateCategory();
            }
            Splash.Visibility = Visibility.Collapsed;
        }

        // Show the update show screen with the show selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateShow popup = new UpdateShow(ref Splash, () => 
            { 
                _categoryChanged = true;
                UpdateCategory(); 
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            for (int i = 0; i < popup.lvShowList.Items.Count; i++)
            {
                ShowDeserialized show = (ShowDeserialized)popup.lvShowList.Items[i];
                if (show.Id.ToString() == (sender as Button).Tag.ToString())
                {
                    popup.lvShowList.SelectedIndex = i;
                }
            }
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Remove the show from the category list and the category from the list for the show
        private void btnRemoveShowFromCategory_Click(object sender, RoutedEventArgs e)
        {
            int showId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Get<ShowCategory>(_categoryId);
                DatabaseFunctions.RemoveShowFromCategory(showId, category);

                connection.CreateTable<Show>();
                Show show = connection.Get<Show>(showId);
                List<string> categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                categories.Remove(category.Name);
                show.Categories = JsonConvert.SerializeObject(categories);
                connection.Update(show);
            }

            _categoryChanged = true;

            UpdateCategory();
        }

        // When the size of the videos items control changes adjust padding to make it look good with a scroll bar
        private void icVideos_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Round(icVideos.ActualHeight) > Math.Round(scrollVideos.ActualHeight))
            {
                icVideos.Margin = new Thickness(10, 0, 0, 0);
            } else
            {
                icVideos.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        // Popup update show category window
        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateShowCategory popup = new UpdateShowCategory(_categoryId, ref Splash, () =>
            {
                _categoryChanged = true;
                UpdateCategory();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Remove the category from the database and from all show category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Get<ShowCategory>(_categoryId);
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    connection.CreateTable<Show>();
                    List<Show> shows = connection.Table<Show>().ToList();
                    foreach (Show show in shows)
                    {
                        List<string> categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                        categories.Remove(category.Name);
                        show.Categories = JsonConvert.SerializeObject(categories);
                        connection.Update(show);
                    }
                    connection.Delete<ShowCategory>(_categoryId);
                    deleted = true;
                }
                Splash.Visibility = Visibility.Collapsed;
            }

            if (deleted)
            {
                _splash.Visibility = Visibility.Collapsed;
                _callback();
                parentWindow.removeChild(this);
                Close();
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
