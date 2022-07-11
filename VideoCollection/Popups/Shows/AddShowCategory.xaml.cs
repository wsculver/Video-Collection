using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.CustomTypes;
using VideoCollection.Helpers;
using VideoCollection.Shows;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for AddShowCategory.xaml
    /// </summary>
    public partial class AddShowCategory : Window, ScaleableWindow
    {
        private List<int> _selectedShowIds;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddShowCategory() { }

        public AddShowCategory(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;
            _selectedShowIds = new List<int>();

            UpdateShowList();

            txtCategoryName.Focus();

            WidthScale = 0.35;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.299;
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
                            CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message + ".", CustomMessageBox.MessageBoxType.OK);
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
        }

        // Close window on cancel
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
            if (txtCategoryName.Text == "")
            {
                ShowOKMessageBox("You need to enter a category name");
            }
            else 
            { 
                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<ShowCategory>();
                    List<ShowCategory> categories = connection.Table<ShowCategory>().ToList();
                    foreach (ShowCategory showCategory in categories)
                    {
                        if (showCategory.Name == txtCategoryName.Text.ToUpper())
                        {
                            repeat = true;
                            break;
                        }
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A category with that name already exists");
                    }
                    else
                    {
                        connection.CreateTable<Show>();
                        foreach (int id in _selectedShowIds)
                        {
                            Show show = connection.Get<Show>(id);

                            // Add category to selected show
                            List<string> showCategories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                            showCategories.Add(txtCategoryName.Text.ToUpper());
                            show.Categories = JsonConvert.SerializeObject(showCategories);
                            connection.Update(show);
                        }

                        _selectedShowIds.Sort();

                        ShowCategory category = new ShowCategory()
                        {
                            Name = txtCategoryName.Text.ToUpper(),
                            ShowIds = JsonConvert.SerializeObject(_selectedShowIds),
                            IsChecked = false
                        };

                        connection.Query<ShowCategory>("CREATE TRIGGER IF NOT EXISTS updatePosition AFTER INSERT ON ShowCategory BEGIN UPDATE ShowCategory SET Position = new.Id WHERE Id = new.Id; END; ");
                        connection.Insert(category);
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


        // Select a show
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedShowIds.Add((int)(sender as CheckBox).Tag);
        }

        // Unselect a show
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedShowIds.Remove((int)(sender as CheckBox).Tag);
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddShowCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addShowCategoryWindow, 500f, 350f);
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
