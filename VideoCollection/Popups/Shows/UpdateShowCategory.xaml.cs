using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Shows;
using VideoCollection.CustomTypes;
using System.Windows.Data;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for RenameShowCategory.xaml
    /// </summary>
    public partial class UpdateShowCategory : Window, ScaleableWindow
    {
        private List<int> _selectedShowIds;
        private string _originalCategoryName;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public UpdateShowCategory() { }

        public UpdateShowCategory(int id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _selectedShowIds = new List<int>();

            Tag = id;
            _splash = splash;
            _callback = callback;

            WidthScale = 0.35;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.299;

            // Check current shows and fill the category name text box
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<ShowCategory>();
                ShowCategory showCategory = connection.Get<ShowCategory>(Tag);
                txtCategoryName.Text = showCategory.Name;
                _originalCategoryName = showCategory.Name;
                ShowCategoryDeserialized showCategoryDeserialized = new ShowCategoryDeserialized(showCategory);

                connection.CreateTable<Show>();
                List<Show> rawShows = connection.Table<Show>().ToList();
                List<ShowDeserialized> shows = new List<ShowDeserialized>();
                foreach (ShowDeserialized showDeserialized in showCategoryDeserialized.Shows)
                {
                    showDeserialized.IsChecked = true;
                    _selectedShowIds.Add(showDeserialized.Id);
                    shows.Add(showDeserialized);
                }
                foreach (Show show in rawShows)
                {
                    if (_selectedShowIds.Contains(show.Id))
                    {
                        continue;
                    }

                    try
                    {
                        ShowDeserialized showDeserialized = new ShowDeserialized(show);
                        shows.Add(showDeserialized);
                    }
                    catch (Exception ex)
                    {
                        Messages.Error(ex.Message, ref Splash);
                        _callback();
                    }
                }
                shows.Sort();
                lvShowList.ItemsSource = shows;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvShowList.ItemsSource);
            view.Filter = ShowFilter;
            txtFilter.IsReadOnly = false;
            txtFilter.Focusable = true;
            txtFilter.IsHitTestVisible = true;

            txtCategoryName.Focus();
        }

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text == "")
            {
                Messages.ShowOKMessageBox("You need to enter a category name", ref Splash);
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
                        if (showCategory.Name != _originalCategoryName && showCategory.Name == txtCategoryName.Text.ToUpper())
                        {
                            repeat = true;
                            break;
                        }
                    }

                    if (repeat)
                    {
                        Messages.ShowOKMessageBox("A category with that name already exists", ref Splash);
                    }
                    else
                    {
                        ShowCategory result = connection.Get<ShowCategory>((int)Tag);
                        _selectedShowIds.Sort();
                        DatabaseFunctions.UpdateCategoryInShows(result.Name, txtCategoryName.Text.ToUpper(), _selectedShowIds);
                        result.Name = txtCategoryName.Text.ToUpper();
                        result.ShowIds = JsonConvert.SerializeObject(_selectedShowIds);
                        connection.Update(result);
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

        // Add show to selected
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedShowIds.Add((int)(sender as CheckBox).Tag);
        }

        // Remove show from selected
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedShowIds.Remove((int)(sender as CheckBox).Tag);
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<UpdateShowCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(updateShowCategoryWindow, 500f, 350f);
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
