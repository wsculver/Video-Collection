using Microsoft.Win32;
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
using VideoCollection.Shows;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using System.IO;
using VideoCollection.Subtitles;
using System.Drawing.Imaging;
using VideoCollection.Animations;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for AddShow.xaml
    /// </summary>
    public partial class AddShow : Window, ScaleableWindow
    {
        private List<ShowCategoryDeserialized> _categories;
        private List<string> _selectedCategories;
        private Show _show;
        private string _rating = "";
        private Border _splash;
        private Action _callback;

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

            WidthScale = 0.43;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.058;

            // Load categories to display
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                List<ShowCategory> rawCategories = (connection.Table<ShowCategory>().ToList()).OrderBy(c => c.Name).ToList();
                _categories = new List<ShowCategoryDeserialized>();
                foreach (ShowCategory category in rawCategories)
                {
                    _categories.Add(new ShowCategoryDeserialized(category));
                }
                icCategories.ItemsSource = _categories;
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
            if (txtShowFolder.Text == "")
            {
                ShowOKMessageBox("You need to select a show folder");
            }
            else if (txtShowName.Text == "")
            {
                ShowOKMessageBox("You need to enter a show name");
            }
            else if (txtFile.Text == "")
            { 
                ShowOKMessageBox("You need to select a show file");
            }
            else if (_rating == "")
            {
                ShowOKMessageBox("You need to select a rating");
            }
            else
            {
                string thumbnail = "";
                if (imgThumbnail.Source == null)
                {
                    ImageSource image = StaticHelpers.CreateThumbnailFromVideoFile(txtFile.Text, TimeSpan.FromSeconds(60));
                    thumbnail = StaticHelpers.ImageSourceToBase64(image);
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
                    BonusSections = _show.BonusSections,
                    BonusVideos = _show.BonusVideos,
                    Rating = _rating,
                    Categories = JsonConvert.SerializeObject(_selectedCategories),
                    IsChecked = false
                };

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Show>();
                    List<Show> shows = connection.Table<Show>().ToList();
                    foreach (Show m in shows)
                    {
                        if (m.Title == txtShowName.Text.ToUpper())
                            repeat = true;
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A show with that name already exists");
                    }
                    else
                    {

                        connection.CreateTable<Show>();
                        connection.Insert(show);

                        connection.CreateTable<ShowCategory>();
                        List<ShowCategory> categories = (connection.Table<ShowCategory>().ToList()).OrderBy(c => c.Name).ToList();
                        foreach (ShowCategory category in categories)
                        {
                            if (_selectedCategories.Contains(category.Name))
                            {
                                DatabaseFunctions.AddShowToCategory(show, category);
                            }
                        }
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

        // Choose show file
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog filePath = StaticHelpers.CreateVideoFileDialog();
            if (filePath.ShowDialog() == true)
            {
                txtFile.Text = StaticHelpers.GetRelativePathStringFromCurrent(filePath.FileName);
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
                Task.Run(async () =>
                {
                    _show = await StaticHelpers.ParseShowVideos(dlg.FileName);
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                       (Action)(() =>
                       {
                           txtShowName.Text = _show.Title;
                           if (_show.Thumbnail != "")
                           {
                               imgThumbnail.Source = StaticHelpers.Base64ToImageSource(_show.Thumbnail);
                           }

                           panelShowFields.Visibility = Visibility.Visible;

                           loadingControl.Visibility = Visibility.Collapsed;
                       }));
                });
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
