using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core
{
	class LauncherUpdater
	{
		public UpdateStatus Status { get; set; }
		public SemanticVersion LatestVersion { get; set; }
		public SemanticVersion CurrentVersion { get; set; }

		public async Task CheckForUpdateAsync(CancellationToken cancellationToken)
		{
			await Task.Delay(10);
		}
	}
}
