using System;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Helpers;
using VideoCollection.CustomTypes;
using System.Windows.Interop;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for YesNoMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window, ScaleableWindow
    {
        public enum MessageBoxType { YesNo, OK };

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public CustomMessageBox() { }

        public CustomMessageBox(string Message, MessageBoxType type)
        {
            InitializeComponent();

            txtMessage.Text = Message;

            WidthScale = 0.25;
            HeightScale = 0.26;
            HeightToWidthRatio = 0.55;

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
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            DialogResult = false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            DialogResult = true;
        }

        public void scaleWindow(Window parent)
        {
            Width = parent.ActualWidth * WidthScale;
            Height = Width * HeightToWidthRatio;
            if(Height > parent.ActualHeight * HeightScale)
            {
                Height = parent.ActualHeight * HeightScale;
                Width = Height / HeightToWidthRatio;
            }

            Left = parent.Left + (parent.Width - ActualWidth) / 2;
            Top = parent.Top + (parent.Height - ActualHeight) / 2;
        }

        private void customMessageBoxWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ForceSoftwareRendering)
            {
                HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                HwndTarget hwndTarget = hwndSource.CompositionTarget;
                hwndTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }
    }
}
