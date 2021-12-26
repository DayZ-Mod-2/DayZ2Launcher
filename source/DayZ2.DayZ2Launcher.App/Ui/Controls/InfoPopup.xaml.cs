using System;
using System.Diagnostics;
using System.Windows;

namespace DayZ2.DayZ2Launcher.App.Ui.Controls
{
    /// <summary>
    ///     Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup : Window
    {
        public InfoPopup(string title, Window owner)
        {
            InitializeComponent();
            Owner = owner;
            Title = title;
        }

        public void SetMessage(string message)
        {
            Message.Text = message;
        }

        public void SetWidth(int width)
        {
            ContentGrid.Width = width;
            InfoWindow.Width = width;
        }

        public void OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void SetLink(string url, string text = null)
        {
            LinkBlock.Visibility = Visibility.Visible;
            if (text == null)
                URLText.Text = url;
            else
                URLText.Text = text;

            URL.NavigateUri = new Uri(url);
        }

        private void URL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(URL.NavigateUri.ToString());
        }
    }
}