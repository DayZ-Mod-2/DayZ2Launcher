using zombiesnu.DayZeroLauncher.App.Core;
namespace zombiesnu.DayZeroLauncher.App.Ui.ServerList
{
	public class ServerListViewModel : ViewModelBase
	{
		public ServerListViewModel()
		{
			Title = "Servers";

			FiltersViewModel = new FiltersViewModel();
			ListViewModel = new ListViewModel();

			FiltersViewModel.Filter.PublishFilter();
		}

		public FiltersViewModel FiltersViewModel { get; set; }
		public ListViewModel ListViewModel { get; set; }

        public override string ToString() { return ""; }

		private GameLauncher _launcher = null;
		public GameLauncher Launcher
		{
			get { return _launcher; }
			set
			{
				_launcher = value;
				PropertyHasChanged("Launcher");
			}
		}
	}
}