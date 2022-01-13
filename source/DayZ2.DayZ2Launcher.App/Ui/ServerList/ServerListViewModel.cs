using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	public class ServerListViewModel : ViewModelBase
	{
		private GameLauncher_old _launcher;

		public ServerListViewModel()
		{
			Title = "Servers";

			FiltersViewModel = new FiltersViewModel();
			ListViewModel = new ListViewModel();

			FiltersViewModel.Filter.PublishFilter();
		}

		public FiltersViewModel FiltersViewModel { get; set; }
		public ListViewModel ListViewModel { get; set; }

		public GameLauncher_old Launcher
		{
			get => _launcher;
			set
			{
				_launcher = value;
				PropertyHasChanged("Launcher");
			}
		}

		public override string ToString()
		{
			return "";
		}
	}
}
