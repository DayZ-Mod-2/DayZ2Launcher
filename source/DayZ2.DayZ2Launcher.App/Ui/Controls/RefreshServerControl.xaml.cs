using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.Controls
{
	public partial class RefreshServerControl : UserControl
	{
		DoubleAnimation m_rotation;
		UIElement m_rotationElement;

		public RefreshServerControl()
		{
			InitializeComponent();

			m_rotation = new()
			{
				From = 0,
				To = -360,
				Duration = new Duration(TimeSpan.FromSeconds(1)),
				RepeatBehavior = RepeatBehavior.Forever,
			};

			m_rotationElement = (UIElement)FindName("Rotator");

			DataContextChanged += (object sender, DependencyPropertyChangedEventArgs e) =>
			{
				if (e.OldValue is IRefreshable oldValue)
				{
					oldValue.PropertyChanged -= DataContext_PropertyChanged;
				}
				if (e.NewValue is IRefreshable newValue)
				{
					newValue.PropertyChanged += DataContext_PropertyChanged;
				}
			};
		}

		void DataContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IRefreshable.IsRefreshing))
			{
				var animation = ((IRefreshable)sender).IsRefreshing ? m_rotation : null;
				m_rotationElement.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
			}
		}


		void Button_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is IRefreshable refreshable)
				refreshable.Refresh();
		}
	}
}
