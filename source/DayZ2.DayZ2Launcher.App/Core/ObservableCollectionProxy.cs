using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class ObservableCollectionProxy<T, U> : ObservableCollection<T>
	{
		Func<U, T> m_create;

		public ObservableCollectionProxy(ObservableCollection<U> collection, Func<U, T> create)
		{
			m_create = create;
			collection.CollectionChanged += Collection_CollectionChanged;
		}

		void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						var newItems = e.NewItems.Cast<U>().Select(n => m_create(n));
						foreach ((T x, int i) in newItems.WithIndex())
						{
							base.InsertItem(e.NewStartingIndex + i, x);
						}
					}
					break;

				case NotifyCollectionChangedAction.Remove:
					for (int i = e.OldItems.Count; i >= 0; --i)
					{
						base.RemoveItem(e.OldStartingIndex + i);
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					base.Clear();
					break;

				default:
					throw new NotImplementedException();
			}
		}
	}
}
