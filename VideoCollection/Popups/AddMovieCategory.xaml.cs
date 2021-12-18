using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoCollection.Movies;
using VideoCollection.Views;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for AddMovieCategory.xaml
    /// </summary>
    public partial class AddMovieCategory : Window
    {
        public AddMovieCategory()
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
                JavaScriptSerializer jss = new JavaScriptSerializer();

                MovieCategory category = new MovieCategory()
                {
                    Name = txtCategoryName.Text.ToUpper(),
                    Movies = jss.Serialize(new List<Movie>()),
                    IsChecked = false
                };

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    List<MovieCategory> categories = (connection.Table<MovieCategory>().ToList()).ToList();
                    foreach(MovieCategory movieCategory in categories)
                    {
                        Console.WriteLine(movieCategory.Id);
                        if(movieCategory.Name == category.Name)
                            repeat = true;
                    }

                    if(repeat)
                    {
                        MessageBox.Show("A category with that name already exists", "Duplicate Category", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        connection.CreateTable<MovieCategory>();
                        connection.Insert(category);
                    }
                }

                if (!repeat)
                {
                    Close();
                }
            }
            else
            {
                MessageBox.Show("You need to enter a category name", "Missing Category Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
