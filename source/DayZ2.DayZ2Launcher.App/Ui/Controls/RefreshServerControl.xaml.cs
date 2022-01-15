using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Controls
{
	public partial class RefreshServerControl : UserControl
	{
		public RefreshServerControl()
		{
			InitializeComponent();
		}

		void Button_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is IRefreshable refreshable)
			{
				refreshable.Refresh();
			}
		}
	}
}
