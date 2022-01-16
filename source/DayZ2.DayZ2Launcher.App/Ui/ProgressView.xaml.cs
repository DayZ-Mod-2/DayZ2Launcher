using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	/// <summary>
	/// Interaction logic for ProgressView2.xaml
	/// </summary>
	public partial class ProgressView2 : Window
	{
		public ProgressView2()
		{
			InitializeComponent();
		}

		public void Ok_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
