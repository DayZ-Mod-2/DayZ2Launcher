using System.ComponentModel;

interface IRefreshable : INotifyPropertyChanged
{
	bool IsRefreshing { get; }
	void Refresh();
}
