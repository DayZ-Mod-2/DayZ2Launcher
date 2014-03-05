using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using NLog;

namespace zombiesnu.DayZeroLauncher.Updater
{
	public partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

            if (e.Args.Length == 0)
            {
                MessageBox.Show("The DayZero Updater application should not be run manually.");
                Environment.Exit(0);
            }
		}
	}
}
