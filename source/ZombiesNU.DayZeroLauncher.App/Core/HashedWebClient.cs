using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	class HashedWebClient : IDisposable
	{
		private WebClient _wc;
		public HashedWebClient()
		{
			_wc = new WebClient();
		}

		public void Dispose()
		{
			if (_wc != null)
			{
				_wc.Dispose();
				_wc = null;
			}
		}

		private string _outFilename;	
		public string OutputFileName
		{
			get { return _outFilename; }
			set { _outFilename = value; }
		}
	}
}
