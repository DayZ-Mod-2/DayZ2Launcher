using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Caliburn.Micro;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
    public class ListViewModel : ViewModelBase,
        IHandle<FilterUpdated>,
        IHandle<ServerUpdated>
    {
        private readonly ObservableCollection<Server> _rawServers = new ObservableCollection<Server>();
        private Func<Server, bool> _filter;
        private ListCollectionView _servers;

        public ListViewModel()
        {
            ReplaceServers();
            Title = "servers";
        }

        public ObservableCollection<Server> RawServers => _rawServers;

        public ListCollectionView Servers
        {
            get => _servers;
            set
            {
                _servers = value;
                OnPropertyChanged(new[] { "Servers" });
            }
        }

        public void Handle(FilterUpdated message)
        {
            _filter = message.Filter;
            Servers.Refresh();
        }

        public void Handle(ServerUpdated message)
        {
            if (message.SuppressRefresh)
                return;

            Server theServer = message.Server;
            _rawServers.Remove(theServer);

            if (!message.IsRemoved && !IsFiltered(theServer))
                _rawServers.Add(theServer);
        }

        private void ReplaceServers()
        {
            Servers = (ListCollectionView)CollectionViewSource.GetDefaultView(_rawServers);
            Servers.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            Servers.Filter = Filter;
        }

        private bool Filter(object obj)
        {
            var server = (Server)obj;

            //if(_filter != null)
            //	return _filter(server);
            return true;
        }

        private static bool IsFiltered(Server server)
        {
            if (server.Name.Contains("- AU") && !UserSettings.Current.IncludeAU)
                return true;
            if (server.Name.Contains("- US") && !UserSettings.Current.IncludeUS)
                return true;
            if ((server.Name.Contains("- SE") || server.Name.Contains("- DE")) && !UserSettings.Current.IncludeEU)
                return true;
            return false;
        }
    }
}