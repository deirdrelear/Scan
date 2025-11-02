using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Common.WPF.Controls.TreeList
{
	public static class SortBehavior
	{
		public static readonly DependencyProperty CanUserSortColumnsProperty = DependencyProperty.RegisterAttached("CanUserSortColumns", typeof(bool), typeof(SortBehavior), new FrameworkPropertyMetadata(OnCanUserSortColumnsChanged));
		public static readonly DependencyProperty CanUseSortProperty = DependencyProperty.RegisterAttached("CanUseSort", typeof(bool), typeof(SortBehavior), new FrameworkPropertyMetadata(true));
		public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.RegisterAttached("SortDirection", typeof(ListSortDirection?), typeof(SortBehavior));
		public static readonly DependencyProperty SortComparerProperty = DependencyProperty.RegisterAttached("SortComparer", typeof(IItemComparer), typeof(SortBehavior));
		public static readonly DependencyProperty DefaultSortComparerProperty = DependencyProperty.RegisterAttached("DefaultSortComparer", typeof(IItemComparer), typeof(SortBehavior), new FrameworkPropertyMetadata(OnDefaultSortComparerChanged));

		[AttachedPropertyBrowsableForType(typeof(ListView))]
		public static bool GetCanUserSortColumns(ListView element)
		{
			return (bool)element.GetValue(CanUserSortColumnsProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(ListView))]
		public static void SetCanUserSortColumns(ListView element, bool value)
		{
			element.SetValue(CanUserSortColumnsProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(ListView))]
		public static IItemComparer GetDefaultSortComparer(ListView element)
		{
			return (IItemComparer)element.GetValue(DefaultSortComparerProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(ListView))]
		public static void SetDefaultSortComparer(ListView element, IItemComparer value)
		{
			element.SetValue(DefaultSortComparerProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static bool GetCanUseSort(GridViewColumn element)
		{
			if (element == null)
				return false;
			return (bool)element.GetValue(CanUseSortProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetCanUseSort(GridViewColumn element, bool value)
		{
			element.SetValue(CanUseSortProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static ListSortDirection? GetSortDirection(GridViewColumn element)
		{
			return (ListSortDirection?)element.GetValue(SortDirectionProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetSortDirection(GridViewColumn element, ListSortDirection? value)
		{
			element.SetValue(SortDirectionProperty, value);
		}

		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static IItemComparer GetSortComparer(GridViewColumn element)
		{
			return (IItemComparer)element.GetValue(SortComparerProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(GridViewColumn))]
		public static void SetSortComparer(GridViewColumn element, IItemComparer value)
		{
			element.SetValue(SortComparerProperty, value);
		}

		static void OnCanUserSortColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var listView = (ListView)d;
			if ((bool)e.NewValue)
			{
				listView.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
				if (listView.IsLoaded)
					DoInitialSort(listView);
				else
					listView.Loaded += OnLoaded;
			}
			else
				listView.RemoveHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnColumnHeaderClick);
		}
		static void OnDefaultSortComparerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var listView = (ListView)d;
			if (e.NewValue != null)
			{
				if (listView.IsLoaded)
					DoInitialSort(listView);
				else
					listView.Loaded += OnLoaded;
			}
		}

		static void OnLoaded(object sender, RoutedEventArgs e)
		{
			var listView = (ListView)e.Source;
			listView.Loaded -= OnLoaded;
			DoInitialSort(listView);
		}

		static void DoInitialSort(ListView listView)
		{
			var defaultComparer = GetDefaultSortComparer(listView);
			if (defaultComparer != null)
				DoSort(listView, defaultComparer);
			var gridView = (GridView)listView.View;
			var column = gridView.Columns.FirstOrDefault(c => GetSortDirection(c) != null);
			if (column != null)
				DoSort(listView, column);
		}

		static void OnColumnHeaderClick(object sender, RoutedEventArgs e)
		{
			var columnHeader = e.OriginalSource as GridViewColumnHeader;
			if (columnHeader != null && GetCanUseSort(columnHeader.Column))
				DoSort((ListView)e.Source, columnHeader.Column);
		}

		static void DoSort(ListView listView, GridViewColumn column)
		{
			var tree = (TreeList)listView;
			if (tree.Root.SortColumn != null)
				SetSortDirection(tree.Root.SortColumn, null);
			var comparer = GetSortComparer(column);
			if (comparer != null)
			{
				tree.Root.RunSort(column, comparer);
				SetSortDirection(column, tree.Root.SortDirection);
			}
		}
		static void DoSort(ListView listView, IItemComparer comparer)
		{
			var tree = (TreeList)listView;
			if (tree != null && comparer != null)
				tree.Root.RunSort(comparer);
		}

		public static void DoSort(ListView listView)
		{
			var comparer = GetDefaultSortComparer(listView);
			DoSort(listView, comparer);
		}
	}
}
