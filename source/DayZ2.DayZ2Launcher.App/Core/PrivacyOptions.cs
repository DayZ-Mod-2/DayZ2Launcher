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
	public class PrivacyOptions : ViewModelBase
	{
		[DataMember]
		private bool m_allowSendingCrashLogs = true;
		public bool AllowSendingCrashLogs
		{
			get => m_allowSendingCrashLogs;
			set => SetValue(ref m_allowSendingCrashLogs, value);
		}
	}
}
