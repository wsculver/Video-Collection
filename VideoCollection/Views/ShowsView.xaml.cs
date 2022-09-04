using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VideoCollection.CustomTypes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Shows;
using VideoCollection.Popups;
using VideoCollection.Popups.Shows;

namespace VideoCollection.Views
{
    /// <summary>
    /// Interaction logic for ShowsView.xaml
    /// </summary>
    public partial class ShowsView : UserControl
    {
        private const int _sideMargins = 24;
        private const int _scrollViewerMargins = 24;
        private double _scrollDistance = 0;
        private List<ShowCategoryDeserialized> _categories;

        public ShowsView()
        {
            InitializeComponent();

            UpdateCategoryDisplay();
        }

        // Refresh category display to show current database data
        private void UpdateCategoryDisplay()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> rawCategories = connection.Table<ShowCategory>().OrderBy(c => c.Position).ToList();
                _categories = new List<ShowCategoryDeserialized>();
                foreach (ShowCategory category in rawCategories)
                {
                    _categories.Add(new ShowCategoryDeserialized(category));
                }

                icCategoryDisplay.ItemsSource = _categories;
            }

            UpdateCategoryScrollButtons();
        }

        // Popup add category window
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            AddShowCategory popup = new AddShowCategory(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Popup add show window
        private void btnNewShow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            AddShow popup = new AddShow(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Popup add bulk movies window
        private void btnAddBulkShows_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            AddBulkShows popup = new AddBulkShows(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Remove the category from the database and from all show category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            int categoryId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Get<ShowCategory>(categoryId);
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                parentWindow.Splash.Visibility = Visibility.Visible;
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

                    int categoryPosition = category.Position;
                    List<ShowCategory> showCategories = connection.Table<ShowCategory>().OrderByDescending(c => c.Position).ToList();
                    foreach (ShowCategory cat in showCategories)
                    {
                        if (cat.Position == categoryPosition)
                        {
                            break;
                        }
                        cat.Position -= 1;
                        connection.Update(cat);
                    }

                    connection.Delete<ShowCategory>(categoryId);
                    deleted = true;
                }
                parentWindow.Splash.Visibility = Visibility.Collapsed;
            }

            if (deleted)
            {
                UpdateCategoryDisplay();
            }
        }

        // Popup update show category window
        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateShowCategory popup = new UpdateShowCategory((int)(sender as Button).Tag, ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Remove the show from the category list and the category from the list for the show
        private void btnRemoveShowFromCategory_Click(object sender, RoutedEventArgs e)
        {
            string[] split = (sender as Button).Tag.ToString().Split(',');
            string categoryId = split[0];
            string showId = split[1];
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory category = connection.Get<ShowCategory>(categoryId);
                connection.CreateTable<Show>();
                Show show = connection.Get<Show>(showId);

                List<string> categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                categories.Remove(category.Name);
                show.Categories = JsonConvert.SerializeObject(categories);
                connection.Update(show);
                DatabaseFunctions.RemoveShowFromCategory(show.Id, category);
            }

            UpdateCategoryDisplay();
        }

        // Popup update show window
        private void btnUpdateExistingShow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateShow popup = new UpdateShow(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Shift a category up by one
        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int categoryId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> categories = connection.Table<ShowCategory>().OrderBy(c => c.Position).ToList();

                ShowCategory previousCategory = categories[0];
                foreach (ShowCategory category in categories)
                {
                    if (category.Id == categoryId)
                    {
                        if (category != previousCategory)
                        {
                            category.Position -= 1;
                            previousCategory.Position += 1;
                            connection.Update(category);
                            connection.Update(previousCategory);
                        }
                        break;
                    }
                    previousCategory = category;
                }
            }

            UpdateCategoryDisplay();
        }

        // Shift a category down by one
        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int categoryId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> categories = connection.Table<ShowCategory>().OrderByDescending(c => c.Position).ToList();

                ShowCategory previousCategory = categories[0];
                foreach (ShowCategory category in categories)
                {
                    if (category.Id == categoryId)
                    {
                        if (category != previousCategory)
                        {
                            category.Position += 1;
                            previousCategory.Position -= 1;
                            connection.Update(category);
                            connection.Update(previousCategory);
                        }
                        break;
                    }
                    previousCategory = category;
                }
            }

            UpdateCategoryDisplay();
        }

        // Left arrow button to scroll left inside a category
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>((sender as Button).Parent);
            double location = scroll.HorizontalOffset;

            if (Math.Round(location - _scrollDistance) <= 0)
            {
                location = 0;
            }
            else
            {
                location -= _scrollDistance;
            }

            var scrolling = new DoubleAnimation(scroll.ContentHorizontalOffset, location, new Duration(TimeSpan.FromMilliseconds(400)));
            scroll.BeginAnimation(AnimatedScrollViewer.SetableOffsetProperty, scrolling);
        }

        // Right arrow button to scroll right inside a category
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>((sender as Button).Parent);
            double location = scroll.HorizontalOffset;

            if (Math.Round(location + _scrollDistance) >= Math.Round(scroll.ScrollableWidth))
            {
                location = scroll.ScrollableWidth;
            }
            else
            {
                location += _scrollDistance;
            }

            var scrolling = new DoubleAnimation(scroll.ContentHorizontalOffset, location, new Duration(TimeSpan.FromMilliseconds(400)));
            scroll.BeginAnimation(AnimatedScrollViewer.SetableOffsetProperty, scrolling);
        }

        // Prevent background scrolling from stopping when the mouse is over a category
        private void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        // Show the show details when a show thumbnail is clicked
        private void showTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                ShowDetails popup = new ShowDetails((int)(sender as Grid).Tag, ref parentWindow.Splash, () => { });
                popup.scaleWindow(parentWindow);
                popup.Owner = parentWindow;
                parentWindow.addChild(popup);
                parentWindow.Splash.Visibility = Visibility.Visible;
                popup.Show();
                popup.Activate();
            }
        }

        // When the grid size is changed scale the middle column to fit as many tiles as possible
        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double columnWidth = 0;

            for (int i = 0; i < icCategoryDisplay.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    ColumnDefinition middleColumn = c.ContentTemplate.FindName("colMiddle", c) as ColumnDefinition;

                    if (columnWidth == 0)
                    {
                        columnWidth = _scrollViewerMargins;
                        double tileWidth = 142;
                        while (columnWidth + tileWidth + _sideMargins < MainGrid.ActualWidth)
                        {
                            columnWidth += tileWidth;
                        }
                    }

                    middleColumn.Width = new GridLength(columnWidth);
                }
            }

            _scrollDistance = columnWidth - _scrollViewerMargins;

            UpdateCategoryScrollButtons();
        }

        // Make the category scroll buttons visable if they are needed
        private void UpdateCategoryScrollButtons()
        {
            for (int i = 0; i < icCategoryDisplay.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    ItemsControl showsControl = c.ContentTemplate.FindName("icShows", c) as ItemsControl;
                    ContentPresenter c2 = (ContentPresenter)showsControl.ItemContainerGenerator.ContainerFromIndex(0);
                    if (c2 != null)
                    {
                        c2.ApplyTemplate();
                        Image tile = c2.ContentTemplate.FindName("imageThumbnail", c2) as Image;
                        ScrollViewer scrollViewer = c.ContentTemplate.FindName("scrollShows", c) as ScrollViewer;
                        bool needScroll = Math.Round(showsControl.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollViewer.ActualWidth);
                        // Show view all button or not
                        if (needScroll)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnViewAll").Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnViewAll").Visibility = Visibility.Hidden;
                        }

                        // Show next button or not
                        if (needScroll && scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnNext").Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnNext").Visibility = Visibility.Hidden;
                        }

                        // Show previous button or not
                        if (scrollViewer.HorizontalOffset > 0)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnPrevious").Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnPrevious").Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
        }

        private void scrollShows_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateCategoryScrollButtons();
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
                        StaticHelpers.ShowVideoPlayer(popup);
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
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message + ".", CustomMessageBox.MessageBoxType.OK);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                parentWindow.Splash.Visibility = Visibility.Visible;
                popup.ShowDialog();
                parentWindow.Splash.Visibility = Visibility.Collapsed;
            }
        }

        // Show the show details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            ShowDetails popup = new ShowDetails((int)(sender as Button).Tag, ref parentWindow.Splash, () => { });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
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
            parentWindow.Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                DatabaseFunctions.DeleteShow(showId);
                UpdateCategoryDisplay();
            }
            parentWindow.Splash.Visibility = Visibility.Collapsed;
        }

        // Show the update show screen with the show selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateShow popup = new UpdateShow(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
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
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }

        // Popup a window showing all shows in the cateogry
        private void btnViewAll_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            ShowViewAll popup = new ShowViewAll((int)(sender as Button).Tag, ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
            popup.Activate();
        }
    }
}
