using System;
using System.Runtime.Serialization;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	[DataContract]
	public class TorrentOptions : BindableBase
	{
		[DataMember] private bool _disableFastResume;
		[DataMember] private bool _enableUpnp = true;
		[DataMember] private int _listeningPort = 54321;
		[DataMember] private int _maxDlConns = 100;
		[DataMember] private int _maxDlSpeed;
		[DataMember] private int _maxUlSpeed;
		[DataMember] private int _numUlSlots = 10;
		[DataMember] private bool _randomizePort = true;
		[DataMember] private bool _stopSeeding;

		public int ListeningPort
		{
			get { return _listeningPort; }
			set
			{
				int oldValue = _listeningPort;

				if (value > 0 && value < 65536)
					_listeningPort = value;
				else
					_listeningPort = new Random().Next(1, 65536);

				if (_listeningPort != oldValue)
				{
					PropertyHasChanged("ListeningPort");
					UserSettings.Current.Save();
				}
			}
		}

		public bool RandomizePort
		{
			get { return _randomizePort; }
			set
			{
				if (_randomizePort != value)
				{
					_randomizePort = value;
					PropertyHasChanged("RandomizePort");
					UserSettings.Current.Save();
				}
			}
		}

		public bool EnableUpnp
		{
			get { return _enableUpnp; }
			set
			{
				if (_enableUpnp != value)
				{
					_enableUpnp = value;
					PropertyHasChanged("EnableUpnp");
					UserSettings.Current.Save();
				}
			}
		}

		public int MaxDLSpeed
		{
			get { return _maxDlSpeed; }
			set
			{
				int oldValue = _maxDlSpeed;

				if (value > 0)
					_maxDlSpeed = value;
				else
					_maxDlSpeed = 0;

				if (_maxDlSpeed != oldValue)
				{
					PropertyHasChanged("MaxDLSpeed");
					UserSettings.Current.Save();
				}
			}
		}

		public int MaxDLConns
		{
			get { return _maxDlConns; }
			set
			{
				int oldValue = _maxDlConns;

				if (value > 0)
					_maxDlConns = value;
				else
					_maxDlConns = 0;

				if (_maxDlConns != oldValue)
				{
					PropertyHasChanged("MaxDLConns");
					UserSettings.Current.Save();
				}
			}
		}

		public int MaxDLConnsNormalized
		{
			get
			{
				int ourNumConns = MaxDLConns;
				if (ourNumConns <= 0)
					ourNumConns = 1000;

				return ourNumConns;
			}
		}

		public int MaxULSpeed
		{
			get { return _maxUlSpeed; }
			set
			{
				int oldValue = _maxUlSpeed;

				if (value > 0)
					_maxUlSpeed = value;
				else
					_maxUlSpeed = 0;

				if (_maxUlSpeed != oldValue)
				{
					PropertyHasChanged("MaxULSpeed");
					UserSettings.Current.Save();
				}
			}
		}

		public int NumULSlots
		{
			get { return _numUlSlots; }
			set
			{
				int oldValue = _numUlSlots;

				if (value > 0)
					_numUlSlots = value;
				else
					_numUlSlots = 0;

				if (_numUlSlots != oldValue)
				{
					PropertyHasChanged("NumULSlots");
					UserSettings.Current.Save();
				}
			}
		}

		public int NumULSlotsNormalized
		{
			get
			{
				int ourNumSlots = NumULSlots;
				if (ourNumSlots <= 0)
					ourNumSlots = 100;

				return ourNumSlots;
			}
		}

		public bool StopSeeding
		{
			get { return _stopSeeding; }
			set
			{
				if (_stopSeeding != value)
				{
					_stopSeeding = value;
					PropertyHasChanged("StopSeeding");
					UserSettings.Current.Save();
				}
			}
		}

		public bool DisableFastResume
		{
			get { return _disableFastResume; }
			set
			{
				if (_disableFastResume != value)
				{
					_disableFastResume = value;
					PropertyHasChanged("DisableFastResume");
					UserSettings.Current.Save();
				}
			}
		}
	}
}