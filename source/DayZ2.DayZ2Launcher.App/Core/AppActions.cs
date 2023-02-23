using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class BoolSet : DynamicObject, INotifyPropertyChanged, IEnumerable
	{
		public readonly struct AcquireGuard : IDisposable
		{
			readonly BoolSet m_set;

			public AcquireGuard(BoolSet set)
			{
				m_set = set;
			}

			public void Dispose()
			{
				m_set.Release();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		readonly Dictionary<string, bool> m_fields = new();
		int m_refCount = 0;

		public void Add(string field, bool value)
		{
			m_fields.Add(field, value);
		}

		public bool this[string name]
		{
			get => m_refCount == 0 && m_fields[name];

			set
			{
				m_fields[name] = value;

				if (m_refCount == 0)
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}
		}

		public AcquireGuard Acquire()
		{
			if (m_refCount++ == 0)
				AcquireReleaseChanged();

			return new AcquireGuard(this);
		}

		public void Release()
		{
			Debug.Assert(m_refCount > 0);

			if (--m_refCount == 0)
				AcquireReleaseChanged();
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			bool r = m_fields.TryGetValue(binder.Name, out bool value);
			result = m_refCount == 0 && value;
			return r;
		}

		void AcquireReleaseChanged()
		{
			if (PropertyChanged != null)
			{
				foreach ((string k, bool v) in m_fields)
					PropertyChanged(this, new PropertyChangedEventArgs(k));
			}
		}

		public IEnumerator GetEnumerator() => m_fields.GetEnumerator();
	}

	public class AppActions
	{
		public const string CanInstallMod = "CanInstallMod";
		public const string CanVerifyIntegrity = "CanVerifyIntegrity";
		public const string CanCheckForUpdates = "CanCheckForUpdates";
		public const string CanLaunch = "CanLaunch";

		public BoolSet Actions { get; } = new()
		{
			{ CanInstallMod, false },
			{ CanVerifyIntegrity, false },
			{ CanCheckForUpdates, true },
			{ CanLaunch, false }
		};
	}
}
