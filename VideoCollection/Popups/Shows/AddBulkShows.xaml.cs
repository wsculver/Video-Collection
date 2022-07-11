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
using VideoCollection.Animations;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;
using System.Collections.Concurrent;
using System.Threading;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for AddBulkShows.xaml
    /// </summary>
    public partial class AddBulkShows : Window, ScaleableWindow
    {
        private ConcurrentDictionary<string, Show> _shows;
        private List<string> _selectedShowTitles;
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
            _selectedShowTitles = new List<string>();
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

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtRootShowFolder.Text == "")
            {
                ShowOKMessageBox("You need to select a root show folder");
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
                    _shows = StaticHelpers.ParseBulkShows(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
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
                                if (GetWindow(this).Owner != null)
                                {
                                    ShowOKMessageBox("Error: " + ex.Message);
                                }
                                else
                                {
                                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message + ".", CustomMessageBox.MessageBoxType.OK);
                                    popup.scaleWindow(parentWindow);
                                    parentWindow.addChild(popup);
                                    popup.Owner = parentWindow;
                                    popup.ShowDialog();
                                }
                            }
                        }
                        lvShowList.ItemsSource = shows;
                        lvShowList.Visibility = Visibility.Visible;
                        loadingControl.Visibility = Visibility.Collapsed;
                    });
                }, token);
            }
        }

        // Select a show
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedShowTitles.Add((sender as CheckBox).Tag.ToString()); 
        }

        // Unselect a show
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedShowTitles.Remove((sender as CheckBox).Tag.ToString());
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
    }
}
