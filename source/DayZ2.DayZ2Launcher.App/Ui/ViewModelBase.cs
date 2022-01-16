using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	[DataContract]
	public abstract class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private bool m_isSelected;
		private string m_title;

		protected ViewModelBase()
		{
		}

		protected void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (field == null && value == null || (field?.Equals(value) ?? false))
			{
				return;
			}

			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void SetValue<T>(ref T field, T value, Action callback, [CallerMemberName] string propertyName = null)
		{
			if (field == null && value == null || (field?.Equals(value) ?? false))
			{
				return;
			}

			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			callback();
		}

		public string Title
		{
			get => m_title;
			set => SetValue(ref m_title, value);
		}

		public bool IsSelected
		{
			get => m_isSelected;
			set => SetValue(ref m_isSelected, value);
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		protected void OnPropertyChanged(params string[] names)
		{
			if (PropertyChanged != null)
			{
				foreach (string name in names)
					PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}
}
