using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MonoTorrent.Common;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui.Controls
{
    /// <summary>
    /// Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup : Window
    {
        public InfoPopup()
        {
            InitializeComponent();
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
            this.Close();
        }

        public void SetLink(string url, string text=null)
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
            System.Diagnostics.Process.Start(URL.NavigateUri.ToString());
        }
    }
}
