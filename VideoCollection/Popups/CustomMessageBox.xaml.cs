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
using VideoCollection.Helpers;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for YesNoMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxType { YesNo, OK };

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public CustomMessageBox() { }

        public CustomMessageBox(string Message, MessageBoxType type)
        {
            InitializeComponent();

            txtMessage.Text = Message;

            switch(type)
            {
                case MessageBoxType.YesNo:
                    Button btnYes = new Button();
                    btnYes.Content = "Yes";
                    btnYes.Width = 64;
                    btnYes.Margin = new Thickness(0, 0, 20, 0);
                    btnYes.Click += btnYes_Click;

                    Button btnNo = new Button();
                    btnNo.Content = "No";
                    btnNo.Width = 64;
                    btnNo.Margin = new Thickness(20, 0, 0, 0);
                    btnNo.Click += btnNo_Click;

                    panelButtons.Width = 168;
                    panelButtons.Children.Add(btnYes);
                    panelButtons.Children.Add(btnNo);
                    break;
                case MessageBoxType.OK:
                    Button btnOK = new Button();
                    btnOK.Content = "OK";
                    btnOK.Width = 64;
                    btnOK.Click += btnOK_Click;

                    panelButtons.Width = 64;
                    panelButtons.Children.Add(btnOK);
                    break;
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<CustomMessageBox>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(customMessageBoxWindow, 200f, 400f);
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
