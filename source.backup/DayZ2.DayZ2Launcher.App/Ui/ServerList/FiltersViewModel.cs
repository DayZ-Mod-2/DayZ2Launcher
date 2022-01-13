using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
	public class FiltersViewModel : ViewModelBase
	{
		public FiltersViewModel()
		{
			Title = "filters";

			Filter = UserSettings.Current.Filter;
		}

		public Filter Filter { get; set; }
	}
}
