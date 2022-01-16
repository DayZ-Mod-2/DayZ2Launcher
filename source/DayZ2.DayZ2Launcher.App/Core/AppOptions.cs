using System.Runtime.Serialization;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class AppOptions : ViewModelBase
	{
		[DataMember] private bool m_lowPingRate;

		public bool LowPingRate
		{
			get => m_lowPingRate;
			set => SetValue(ref m_lowPingRate, value, UserSettings.Current.Save);
		}
	}
}
