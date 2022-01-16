using System.ComponentModel;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	interface IRefreshable : INotifyPropertyChanged
	{
		bool IsRefreshing { get; }
		void Refresh();
	}
}
