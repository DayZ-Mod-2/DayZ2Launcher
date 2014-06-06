using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	public class PluginsViewModel : ViewModelBase
	{
		private ObservableCollection<MetaPlugin> _availablePlugins = new ObservableCollection<MetaPlugin>();
		private bool _isVisible;
		private ObservableCollection<MetaPlugin> _missingPlugins = new ObservableCollection<MetaPlugin>();

		private MetaPlugin _selectedPlugin;

		public PluginsViewModel()
		{
			Refresh(null);
		}

		public ObservableCollection<MetaPlugin> AvailablePlugins
		{
			get { return _availablePlugins; }
			set
			{
				_availablePlugins = value;
				PropertyHasChanged("AvailablePlugins");
			}
		}

		public MetaPlugin SelectedPlugin
		{
			get { return _selectedPlugin; }
			set
			{
				_selectedPlugin = value;
				PropertyHasChanged("SelectedPlugin");
			}
		}

		public ObservableCollection<MetaPlugin> MissingPlugins
		{
			get { return _missingPlugins; }
			set
			{
				_missingPlugins = value;
				PropertyHasChanged("MissingPlugins");
			}
		}

		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				_isVisible = value;
				PropertyHasChanged("IsVisible");
			}
		}

		public void Refresh(IEnumerable<MetaPlugin> plugins)
		{
			if (plugins == null || plugins.Count() < 1)
			{
				AvailablePlugins.Clear();
				MissingPlugins.Clear();
				foreach (string pluginIdent in UserSettings.Current.EnabledPlugins)
					MissingPlugins.Add(new MetaPlugin(pluginIdent));
			}
			else
			{
				AvailablePlugins.Clear();
				foreach (MetaPlugin avPlugin in plugins)
				{
					avPlugin.IsEnabled =
						(UserSettings.Current.EnabledPlugins.Count(x => x.Equals(avPlugin.Ident, StringComparison.OrdinalIgnoreCase)) > 0);
					AvailablePlugins.Add(avPlugin);
				}

				MissingPlugins.Clear();
				foreach (string pluginIdent in UserSettings.Current.EnabledPlugins)
				{
					if (plugins.Count(x => x.Ident.Equals(pluginIdent, StringComparison.OrdinalIgnoreCase)) < 1)
						MissingPlugins.Add(new MetaPlugin(pluginIdent));
				}
			}
		}

		public void RemoveMissing(MetaPlugin missingEntry)
		{
			MissingPlugins.Remove(missingEntry);
		}

		public void SaveSettings()
		{
			List<string> userList = UserSettings.Current.EnabledPlugins;
			userList.Clear();

			userList.AddRange(AvailablePlugins.Where(x => { return x.IsEnabled; }).Select(y => y.Ident).AsEnumerable());
			userList.AddRange(MissingPlugins.Select(y => y.Ident).AsEnumerable());

			UserSettings.Current.Save();
		}
	}
}