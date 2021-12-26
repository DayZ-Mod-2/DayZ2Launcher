using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.ServerList;

namespace DayZ2.DayZ2Launcher.App.Ui.Controls
{
    /// <summary>
    ///     Interaction logic for ServerListGrid.xaml
    /// </summary>
    public partial class ServerListGrid : UserControl,
        IHandle<RefreshingServersChange>, IHandle<ServerListGrid.LaunchJoinServerEvent>
    {
        private LaunchJoinServerEvent _queuedJoinEvt;

        public ServerListGrid()
        {
            InitializeComponent();
            App.Events.Subscribe(this);
        }

        public void Handle(LaunchJoinServerEvent joinEvt)
        {
            Execute.OnUiThread(() =>
            {
                if (JoinFromEvent(joinEvt) == false) //defer for later when we know of this server
                    _queuedJoinEvt = joinEvt;
            }, Dispatcher, DispatcherPriority.Input);
        }

        public void Handle(RefreshingServersChange message)
        {
            Execute.OnUiThread(() =>
            {
                DataGridColumn column = TheGrid.Columns[5];
                Style originalStyle = column.HeaderStyle;
                var newStyle = new Style(typeof(DataGridColumnHeader), originalStyle);
                newStyle.Setters.Add(new Setter(VisibilityProperty, message.IsRunning ? Visibility.Hidden : Visibility.Visible));
                column.HeaderStyle = newStyle;

                if (_queuedJoinEvt != null && message.IsRunning == false) //maybe now we know of this server...
                {
                    LaunchJoinServerEvent theEvt = _queuedJoinEvt;
                    _queuedJoinEvt = null; //dont try and connect to this server forever, though
                    JoinFromEvent(theEvt);
                }
            }, Dispatcher);
        }

        protected void JoinServer(Server server)
        {
            _queuedJoinEvt = null;

            ServerListView listView = null;
            var parent = (FrameworkElement)Parent;
            do
            {
                if (parent is ServerListView)
                {
                    listView = (ServerListView)parent;
                    break;
                }
                parent = (FrameworkElement)parent.Parent;
            } while (parent != null);

            listView.ViewModel().Launcher.JoinServer(Window.GetWindow(Parent), server);
        }

        private void RowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var control = e.OriginalSource as FrameworkElement;
            if (control != null)
            {
                if (control.Name == "Refresh")
                {
                    e.Handled = true;
                    return;
                }
            }
            var server = (Server)((Control)sender).DataContext;
            JoinServer(server);
        }

        private void RowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                e.Handled = true;
        }

        private void RowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var server = (Server)((Control)sender).DataContext;
                JoinServer(server);
            }
        }

        private void RowLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            App.Events.Publish(new DataGridRowSelected());
        }

        private List<Server> GetServers()
        {
            return ((IEnumerable)TheGrid.DataContext)
                .Cast<Server>().ToList();
        }

        private void RefreshAllServer(object sender, RoutedEventArgs e)
        {
            List<Server> servers = GetServers();
            var batch = new ServerBatchRefresher("Refreshing some servers...", servers);
            App.Events.Publish(new RefreshServerRequest(batch));
        }

        private void RefreshAllServersDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private bool JoinFromEvent(LaunchJoinServerEvent joinEvt)
        {
            List<Server> servers;
            try
            {
                servers = GetServers();
            }
            catch (Exception)
            {
                servers = null;
            }

            Server foundServer = null;
            if (servers != null)
                foundServer = servers.FirstOrDefault(srv => srv.MatchesIpPort(joinEvt.IpAddress, joinEvt.Port));

            if (foundServer == null)
                return false;
            JoinServer(foundServer);
            return true;
        }

        public class LaunchJoinServerEvent : MainWindow.LaunchRoutedCommand
        {
            public string IpAddress;
            public int Port;

            public LaunchJoinServerEvent(string ipAddress, int port, NameValueCollection data, Window mainWnd, string queryString)
                : base(data, mainWnd)
            {
                IpAddress = ipAddress;
                Port = port;
            }
        }
    }

    public class RefreshServerRequest
    {
        public RefreshServerRequest(ServerBatchRefresher batch)
        {
            Batch = batch;
        }

        public ServerBatchRefresher Batch { get; set; }
    }

    public class DataGridRowSelected
    {
    }
}