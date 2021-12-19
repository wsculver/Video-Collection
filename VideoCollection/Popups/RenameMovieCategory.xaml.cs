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
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for RenameMovieCategory.xaml
    /// </summary>
    public partial class RenameMovieCategory : Window
    {
        public RenameMovieCategory()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text != "")
            {
                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    List<MovieCategory> categories = connection.Table<MovieCategory>().ToList();
                    foreach (MovieCategory movieCategory in categories)
                    {
                        if (movieCategory.Name == txtCategoryName.Text.ToUpper())
                            repeat = true;
                    }

                    if (repeat)
                    {
                        MessageBox.Show("A category with that name already exists", "Duplicate Category", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MovieCategory result = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + Tag.ToString())[0];
                        DatabaseFunctions.UpdateCategoryNameInMovies(result.Name, txtCategoryName.Text.ToUpper());
                        result.Name = txtCategoryName.Text.ToUpper();
                        connection.Update(result);
                    }
                }

                if(!repeat)
                    Close();
            }
            else
            {
                MessageBox.Show("You need to enter a category name", "Missing Category Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
