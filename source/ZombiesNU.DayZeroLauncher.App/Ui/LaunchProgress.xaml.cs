using System;
using System.Collections.Generic;
using System.Windows;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	/// <summary>
	///     Interaction logic for LaunchProgress.xaml
	/// </summary>
	public partial class LaunchProgress : Window
	{
		public bool InstallSuccessfull = false;

		public LaunchProgress(Window ownerWnd, MetaGameType gameType, IEnumerable<MetaAddon> addOns)
		{
			Owner = ownerWnd;
			InitializeComponent();

			Loaded += (sender, args) =>
			{
				Activate();
				var ctx = new LaunchProgressViewModel(gameType, addOns);
				ctx.Dispatcher = Dispatcher;
				DataContext = ctx;
				ctx.OnRequestClose += (snd, evt) =>
				{
					InstallSuccessfull = true;
					OK_Click(snd, evt);
				};
			};
		}

		protected LaunchProgressViewModel ViewModel
		{
			get { return (LaunchProgressViewModel) DataContext; }
		}

		public void OK_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}