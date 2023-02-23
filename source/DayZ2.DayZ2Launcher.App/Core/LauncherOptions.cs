using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class LauncherOptions : ViewModelBase
	{
		[DataMember] private bool m_closeOnLaunch = false;
		public bool CloseOnLaunch
		{
			get => m_closeOnLaunch;
			set => SetValue(ref m_closeOnLaunch, value);
		}
	}
}
