using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Popups;

namespace VideoCollection.Helpers
{
    public static class Messages
    {
        // Shows a custom OK message box
        public static void ShowOKMessageBox(string message, ref Border splash)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            splash.Visibility = Visibility.Collapsed;
        }

        public static void Error(string message, ref Border splash, string errorType = "")
        {
            ShowOKMessageBox((errorType.Equals("") ? "" : errorType + " ") + "Error: " + message, ref splash);
        }

        public static void Warning(string message, ref Border splash, string warningType = "")
        {
            ShowOKMessageBox((warningType.Equals("") ? "" : warningType + " ") + "Warning: " + message, ref splash);
        }
    }
}
