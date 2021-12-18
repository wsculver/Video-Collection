using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VideoCollection.Commands;

namespace VideoCollection.ViewModels
{
    internal class MoviesViewModel : BaseViewModel
    {
        public ICommand MovieClickCommand { get; set; }

        public MoviesViewModel()
        {
            MovieClickCommand = new MovieClickCommand(this);
        }
    }
}
