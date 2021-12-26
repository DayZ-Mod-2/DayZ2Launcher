using System.Windows;
using System.Windows.Controls;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui
{
    /// <summary>
    ///     Interaction logic for PluginsView.xaml
    /// </summary>
    public partial class PluginsView : UserControl
    {
        public PluginsView()
        {
            InitializeComponent();
        }

        private PluginsViewModel ViewModel()
        {
            return (PluginsViewModel)DataContext;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            ViewModel().IsVisible = false;
        }

        private void MissingPluginsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dg = (DataGrid)sender;
            dg.UnselectAllCells();
        }

        private void MissingPlugin_Click(object sender, RoutedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            var pe = (MetaPlugin)fe.DataContext;
            ViewModel().RemoveMissing(pe);
        }

        private void PluginEnabledCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            ViewModel().SaveSettings();
        }
    }
}