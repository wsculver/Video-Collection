using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Shows;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using VideoCollection.Animations;
using VideoCollection.CustomTypes;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Media;
using System.Windows.Data;
using VideoCollection.Database;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for AddBulkShows.xaml
    /// </summary>
    public partial class AddBulkShows : Window, ScaleableWindow
    {
        private ConcurrentDictionary<string, Show> _shows;
        private HashSet<string> _selectedShowTitles;
        private Border _splash;
        private CancellationTokenSource _tokenSource;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddBulkShows() { }

        public AddBulkShows(ref Border splash)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _shows = new ConcurrentDictionary<string, Show>();
            _selectedShowTitles = new HashSet<string>();
            _tokenSource = new CancellationTokenSource();

            WidthScale = 0.43;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.058;
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
            if (txtRootShowFolder.Text == "")
            {
                Messages.ShowOKMessageBox("You need to select a root show folder", ref Splash);
            }
            else
            {
                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Show>();

                    foreach (KeyValuePair<string, Show> entry in _shows)
                    {
                        if (_selectedShowTitles.Contains(entry.Key))
                        {
                            connection.Insert(entry.Value);
                            DatabaseFunctions.AddShowToAllCategory(entry.Value.Id, connection);
                            ImageSource thumbnail = StaticHelpers.Base64ToImageSource(entry.Value.Thumbnail);
                            thumbnail.Freeze();
                            App.showThumbnails[entry.Value.Id] = thumbnail;
                        }
                    }
                }

                _splash.Visibility = Visibility.Collapsed;
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                parentWindow.removeChild(this);
                Close();
            }
        }

        // Choose the root show folder which contains show folders
        private void btnChooseRootShowFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = StaticHelpers.CreateFolderFileDialog();
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtRootShowFolder.Text = StaticHelpers.GetRelativePathStringFromCurrent(dlg.FileName);
                loadingControl.Content = new LoadingSpinner();
                loadingControl.Visibility = Visibility.Visible;
                var token = _tokenSource.Token;
                Task.Run(() => 
                {
                    var result = StaticHelpers.ParseBulkShows(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        if (result.IsFailure)
                        {
                            Messages.Error(result.Error, ref Splash, "Parse");
                            if (!result.IsPartial)
                            {
                                txtRootShowFolder.Text = "";
                                loadingControl.Visibility = Visibility.Collapsed;
                                return;
                            }
                        }
                        _shows = result.Value;
                        if (!_shows.IsEmpty)
                        {
                            List<ShowDeserialized> shows = new List<ShowDeserialized>();
                            foreach (KeyValuePair<string, Show> entry in _shows)
                            {
                                try
                                {
                                    ShowDeserialized showDeserialized = new ShowDeserialized(entry.Value);
                                    showDeserialized.IsChecked = true;
                                    shows.Add(showDeserialized);
                                    _selectedShowTitles.Add(entry.Key);
                                }
                                catch (Exception ex)
                                {
                                    Messages.Error(ex.Message, ref Splash);
                                }
                            }
                            shows.Sort();
                            lvShowList.ItemsSource = shows;
                            lvShowList.Visibility = Visibility.Visible;
                            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvShowList.ItemsSource);
                            view.Filter = ShowFilter;
                            selectButtons.Visibility = Visibility.Visible;
                            panelAddShows.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtRootShowFolder.Text = "";
                            Messages.ShowOKMessageBox("No new shows found in that folder.", ref Splash);
                        }
                        loadingControl.Visibility = Visibility.Collapsed;
                    });
                }, token);
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddBulkShows>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addBulkShowsWindow, 500f, 500f);
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

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            lvShowList.SelectAll();
        }

        private void btnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            lvShowList.UnselectAll();
        }

        private void lvShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedShowTitles.Clear();
            foreach (ShowDeserialized show in lvShowList.SelectedItems)
            {
                _selectedShowTitles.Add(show.Title);
            }
        }
    }
}
