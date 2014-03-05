using System;
using System.Windows;
using System.Windows.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Execute
	{
		public static void OnUiThread(Action action, Dispatcher disp = null, DispatcherPriority prio = DispatcherPriority.Background)
		{
			if (disp == null)
			{
				if (Application.Current != null)
					disp = Application.Current.Dispatcher;
			}

			if(disp != null)
				disp.BeginInvoke(action, prio);
		}

		public static void OnUiThreadSync(Action action, Dispatcher disp = null, DispatcherPriority prio = DispatcherPriority.Background)
		{
			if (disp == null)
			{
				if (Application.Current != null)
					disp = Application.Current.Dispatcher;
			}

			if (disp != null)
				disp.Invoke(action, prio);
		} 
	}
}