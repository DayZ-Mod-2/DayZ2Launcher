using System.Runtime.Serialization;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class AppOptions : ViewModelBase
	{
		[DataMember] private bool _lowPingRate;

		public bool LowPingRate
		{
			get { return _lowPingRate; }
			set
			{
				_lowPingRate = value;
				OnPropertyChanged(new[] { "LowPingRate" });
				UserSettings.Current.Save();
			}
		}
	}
}
