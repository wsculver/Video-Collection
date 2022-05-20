using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoCollection.CustomTypes;
using VideoCollection.Helpers;
using VideoCollection.Shows;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for ShowDetails.xaml
    /// </summary>
    public partial class ShowDetails : Window, ScaleableWindow
    {
        private static int _scrollViewerMargins = 24;
        private static int _sideMargins = 16;
        private double _scrollDistance = 0;
        private ShowDeserialized _showDeserialized;
        private Dictionary<string, List<ShowBonusVideoDeserialized>> _bonusVideosDictionary;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public ShowDetails() { }

        public ShowDetails(string Id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;

            WidthScale = 0.93;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.489;

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                Show show = connection.Query<Show>("SELECT * FROM Show WHERE Id = " + Id)[0];
                try
                {
                    _showDeserialized = new ShowDeserialized(show);
                }
                catch (Exception ex)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                    popup.scaleWindow(parentWindow);
                    parentWindow.addChild(popup);
                    popup.Owner = parentWindow;
                    popup.ShowDialog();
                    _callback();
                }
                labelTitle.Content = _showDeserialized.Title;
                imageShowThumbnail.Source = _showDeserialized.Thumbnail;
                txtRating.Text = _showDeserialized.Rating;
                string categories = "";
                _showDeserialized.Categories.Sort();
                foreach(string category in _showDeserialized.Categories)
                {
                    categories += (CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category.ToLower()) + ", ");
                }
                categories = categories.Substring(0, categories.Length-2);
                txtCategories.Text = categories;
                icBonusSectionButtons.ItemsSource = _showDeserialized.BonusSections;
                _bonusVideosDictionary = new Dictionary<string, List<ShowBonusVideoDeserialized>>();
                foreach(ShowBonusVideoDeserialized bonusVideo in _showDeserialized.BonusVideos)
                {
                    if(!_bonusVideosDictionary.ContainsKey(bonusVideo.Section))
                    {
                        _bonusVideosDictionary.Add(bonusVideo.Section, new List<ShowBonusVideoDeserialized>());
                    } 
                    _bonusVideosDictionary[bonusVideo.Section].Add(bonusVideo);
                }
                if (_showDeserialized.BonusSections.Any())
                {
                    _showDeserialized.BonusSections.FirstOrDefault().Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
                    icBonusVideos.ItemsSource = _bonusVideosDictionary[_showDeserialized.BonusSections.FirstOrDefault().Name];
                    separatorBonusTop.Visibility = Visibility.Visible;
                    separatorBonusBottom.Visibility = Visibility.Visible;
                }
            }

            UpdateBonusScrollButtons();
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<ShowDetails>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            double columnWidth = 0;

            if (columnWidth == 0)
            {
                columnWidth = _scrollViewerMargins;
                double tileWidth = 154;
                while (columnWidth + tileWidth + _sideMargins < MainGrid.ActualWidth)
                {
                    columnWidth += tileWidth;
                }
            }

            colMiddle.Width = new GridLength(columnWidth);

            _scrollDistance = columnWidth - _scrollViewerMargins;

            ScaleValue = ScaleValueHelper.CalculateScale(showDetailsWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            _callback();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        private void imageShowThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(_showDeserialized);
                        App.videoPlayer = popup;
                        popup.WidthScale = 1.0;
                        popup.HeightScale = 1.0;
                        popup.HeightToWidthRatio = parentWindow.ActualHeight / parentWindow.ActualWidth;
                        popup.scaleWindow(parentWindow);
                        parentWindow.addChild(popup);
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
                    App.videoPlayer.updateVideo(_showDeserialized);
                }
            }
        }

        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                string[] split = (sender as Image).Tag.ToString().Split(new[] {",,,"}, StringSplitOptions.None);
                ShowBonusVideoDeserialized bonusVideo = new ShowBonusVideoDeserialized(split[0], split[1], split[2], split[3]);
                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(bonusVideo);
                        App.videoPlayer = popup;
                        popup.WidthScale = 1.0;
                        popup.HeightScale = 1.0;
                        popup.HeightToWidthRatio = parentWindow.ActualHeight / parentWindow.ActualWidth;
                        popup.scaleWindow(parentWindow);
                        parentWindow.addChild(popup);
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
                    App.videoPlayer.updateVideo(bonusVideo);
                }
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>(button.Parent);
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

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>(button.Parent);
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

        // Make the bonus scroll buttons visable if they are needed
        private void UpdateBonusScrollButtons()
        {
            ContentPresenter c2 = (ContentPresenter)icBonusVideos.ItemContainerGenerator.ContainerFromIndex(0);
            if (c2 != null)
            {
                c2.ApplyTemplate();
                Image tile = c2.ContentTemplate.FindName("imageThumbnail", c2) as Image;
                if (Math.Round(icBonusVideos.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollBonusVideos.ActualWidth) && scrollBonusVideos.HorizontalOffset < scrollBonusVideos.ScrollableWidth)
                {
                    btnNext.Visibility = Visibility.Visible;
                }
                else
                {
                    btnNext.Visibility = Visibility.Hidden;
                }

                if (scrollBonusVideos.HorizontalOffset > 0)
                {
                    btnPrevious.Visibility = Visibility.Visible;
                }
                else
                {
                    btnPrevious.Visibility = Visibility.Hidden;
                }
            }
        }

        private void scrollBonusVideos_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateBonusScrollButtons();
        }

        private void btnBonusSection_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            for (int i = 0; i < icBonusSectionButtons.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icBonusSectionButtons.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    Button button = c.ContentTemplate.FindName("btnBonusSection", c) as Button;
                    if(button.Equals(clickedButton))
                    {
                        button.Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
                    }
                    else
                    {
                        button.Background = null;
                    }
                }
            }

            scrollBonusVideos.ScrollToHorizontalOffset(0);
            icBonusVideos.ItemsSource = _bonusVideosDictionary[clickedButton.Content.ToString()];

            UpdateBonusScrollButtons();
        }

        private void imageShowThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            playShowIconBackground.Visibility = Visibility.Visible;
            iconPlayShow.Visibility = Visibility.Visible;
            showSplash.Visibility = Visibility.Visible;
        }

        private void imageShowThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            playShowIconBackground.Visibility = Visibility.Collapsed;
            iconPlayShow.Visibility = Visibility.Collapsed;
            showSplash.Visibility = Visibility.Collapsed;
        }

        private void imageThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent, "rectPlayBackground").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayBonus").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "bonusSplash").Visibility = Visibility.Visible;
        }

        private void imageThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent, "rectPlayBackground").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayBonus").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "bonusSplash").Visibility = Visibility.Collapsed;
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
