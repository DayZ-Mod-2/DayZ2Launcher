using System;
using System.Collections.Generic;
using System.Windows;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui
{
    /// <summary>
    ///     Interaction logic for LaunchProgress.xaml
    /// </summary>
    public partial class LaunchProgress : Window
    {
        public bool InstallSuccessful = false;

        public LaunchProgress(Window ownerWnd, MetaGameType gameType)
        {
            Owner = ownerWnd;
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Activate();
                var ctx = new LaunchProgressViewModel(gameType);
                ctx.Dispatcher = Dispatcher;
                this.DataContext = ctx;
                ctx.OnRequestClose += (snd, evt) =>
                {
                    InstallSuccessful = true;
                    OK_Click(snd, evt);
                };
            };
        }

        protected LaunchProgressViewModel ViewModel => (LaunchProgressViewModel)DataContext;

        public void OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
