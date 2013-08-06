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
        bool running = true;
        bool doneRunning = false;
        public InfoPopup()
        {
            InitializeComponent();
            System.Threading.Tasks.Task.Factory.StartNew(() => UpdateMessage());
        }

        public void UpdateMessage()
        {
            while (running)
            {
                Dispatcher.Invoke(new Action<string>(SetMessage), (TorrentUpdater.CurrentState() == TorrentState.Downloading ? "Launcher is downloading missing/broken files .. \nCurrent Speed: " + TorrentUpdater.GetCurrentSpeed() + " kB/s " : "Launcher is checking your files ..\n")
                  + String.Format("Progress: {0:00}%", TorrentUpdater.GetCurrentProgress())
                  + "\nClick OK and try again.");
                System.Threading.Thread.Sleep(1000);
                
            }
            doneRunning = true;
        }

        public void SetMessage(string message)
        {
            Message.Content = message;
        }

        public void OK_Click(object sender, EventArgs e)
        {
            running = false;
            this.Close();
        }
    }
}
