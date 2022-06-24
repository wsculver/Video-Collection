using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private string _categoryId;
        private bool _categoryChanged = false;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public ShowViewAll() { }

        public ShowViewAll(string Id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _categoryId = Id;
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
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Query<ShowCategory>("SELECT * FROM ShowCategory WHERE Id = " + _categoryId)[0];
                ShowCategoryDeserialized categoryDeserialized = new ShowCategoryDeserialized(category);
                labelCategory.Content = categoryDeserialized.Name;
                icVideos.ItemsSource = categoryDeserialized.Shows;
            }
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
                ShowDetails popup = new ShowDetails((sender as Grid).Tag.ToString(), ref Splash, () => { });
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                popup.Show();
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
            string id = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                Show show = connection.Query<Show>("SELECT * FROM Show WHERE Id = " + id)[0];
                try
                {
                    ShowDeserialized showDeserialized = new ShowDeserialized(show);

                    if (App.videoPlayer == null)
                    {
                        MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                        try
                        {
                            VideoPlayer popup = new VideoPlayer(showDeserialized);
                            App.videoPlayer = popup;
                            popup.Width = parentWindow.ActualWidth;
                            popup.Height = parentWindow.ActualHeight;
                            popup.Owner = parentWindow;
                            popup.Left = popup.LeftMultiplier = parentWindow.Left;
                            popup.Top = popup.TopMultiplier = parentWindow.Top;
                            popup.Show();
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
                        App.videoPlayer.updateVideo(showDeserialized);
                    }
                }
                catch (Exception ex)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                    popup.scaleWindow(parentWindow);
                    parentWindow.addChild(popup);
                    popup.Owner = parentWindow;
                    Splash.Visibility = Visibility.Visible;
                    popup.ShowDialog();
                    Splash.Visibility = Visibility.Collapsed;
                    _callback();
                }
            }
        }

        // Show the show details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            ShowDetails popup = new ShowDetails((sender as Button).Tag.ToString(), ref Splash, () => { });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Delete the show from the database
        private void btnDeleteShow_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string showId = button.Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                Show show = connection.Query<Show>("SELECT * FROM Show WHERE Id = " + showId)[0];

                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + show.Title + " from the database? This only removes the show from your video collection, it does not delete any show files.", CustomMessageBox.MessageBoxType.YesNo);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    _categoryChanged = true;
                    DatabaseFunctions.DeleteShow(show);
                    UpdateCategory();
                }
                Splash.Visibility = Visibility.Collapsed;
            }
        }

        // Show the update show screen with the show selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
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
                if (show.Id.ToString() == button.Tag.ToString())
                {
                    popup.lvShowList.SelectedIndex = i;
                }
            }
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Remove the show from the category list and the category from the list for the show
        private void btnRemoveShowFromCategory_Click(object sender, RoutedEventArgs e)
        {
            string showId = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Query<ShowCategory>("SELECT * FROM ShowCategory WHERE Id = " + _categoryId)[0];
                DatabaseFunctions.RemoveShowFromCategory(showId, category);

                connection.CreateTable<Show>();
                Show show = connection.Query<Show>("SELECT * FROM Show WHERE Id = " + showId)[0];
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
        }

        // Remove the category from the database and from all show category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Query<ShowCategory>("SELECT * FROM ShowCategory WHERE Id = " + _categoryId)[0];
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
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
