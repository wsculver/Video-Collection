using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VideoCollection.ViewModels;

namespace VideoCollection.Commands
{
    internal class UpdateViewCommand : ICommand
    {
        private MainViewModel viewModel;
        private ShowsViewModel showsViewModel;
        public MoviesViewModel moviesViewModel;

        public UpdateViewCommand(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        // Switch views
        public void Execute(object parameter)
        {
            if(parameter.ToString() == "Shows")
            {
                if(showsViewModel == null)
                {
                    showsViewModel = new ShowsViewModel();
                }
                viewModel.SelectedViewModel = showsViewModel;
            } 
            else if(parameter.ToString() == "Movies")
            {
                if (moviesViewModel == null)
                {
                    moviesViewModel = new MoviesViewModel();
                }
                viewModel.SelectedViewModel = moviesViewModel;
            }
        }
    }
}
