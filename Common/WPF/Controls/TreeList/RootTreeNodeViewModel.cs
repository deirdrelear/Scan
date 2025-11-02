using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;

namespace Common.WPF.Controls.TreeList
{
	public class RootTreeNodeViewModel : TreeNodeViewModel, ITreeList
	{
		List<ITreeList> _treeLists;

		public RootTreeNodeViewModel()
		{
			_treeLists = new List<ITreeList>();
			Rows = new ObservableCollectionAdv<TreeNodeViewModel>();
			IsExpanded = true;
			IsRoot = true;
			AssignToRoot(this);
		}

		public void AssignToTree(ITreeList treeList)
		{
			_treeLists.Add(treeList);
		}
		public void DisassignFromTree(ITreeList treeList)
		{
			_treeLists.Remove(treeList);
		}
		public void SetSource(IEnumerable source)
		{
			NotifyCollection = source as INotifyCollectionChanged;
			Rows.Clear();
			Nodes.Clear();
			if (source != null)
				foreach (TreeNodeViewModel item in source)
				{
					item.AssignToRoot(this);
					Nodes.Add(item);
				}
		}

		INotifyCollectionChanged _notifyCollection;
		public INotifyCollectionChanged NotifyCollection
		{
			get { return _notifyCollection; }
			set
			{
				if (NotifyCollection != null)
					NotifyCollection.CollectionChanged -= ChildrenChanged;
				_notifyCollection = value;
				if (NotifyCollection != null)
					NotifyCollection.CollectionChanged += ChildrenChanged;
			}
		}

		void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems != null)
					{
						int index = e.NewStartingIndex;
						for (int i = 0; i < e.NewItems.Count; i++)
							Nodes.Insert(index + i, (TreeNodeViewModel)e.NewItems[i]);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					if (e.OldItems != null)
					{
						int index = e.OldStartingIndex;
						for (int i = 0; i < e.OldItems.Count; i++)
							Nodes.RemoveAt(index + i);
					}
					break;
				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Reset:
					SetSource((IEnumerable<TreeNodeViewModel>)NotifyCollection);
					break;
			}
		}

		#region ITreeList Members

		public ObservableCollectionAdv<TreeNodeViewModel> Rows { get; }

		public object SelectedTreeNode
		{
			get
			{
				return _treeLists.Count > 0 ? _treeLists[0].SelectedTreeNode : null;
			}
			set
			{
				_treeLists.ForEach(item =>
				{
					if (item.SelectedTreeNode != value)
						item.SelectedTreeNode = value;
				});
			}
		}

		public void SuspendSelection()
		{
			_treeLists.ForEach(item => item.SuspendSelection());
		}
		public void ResumeSelection()
		{
			_treeLists.ForEach(item => item.ResumeSelection());
		}

		#endregion

		public GridViewColumn SortColumn { get; private set; }
		public ListSortDirection? SortDirection { get; private set; }
		public IItemComparer ItemComparer { get; private set; }
		public void RunSort(GridViewColumn column, IItemComparer itemComparer)
		{
			SortDirection = column == SortColumn ? (SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending;
			SortColumn = column;
			RunSort(itemComparer);
		}
		public void RunSort(IItemComparer itemComparer)
		{
			if (SortDirection == null)
				SortDirection = ListSortDirection.Ascending;
			ItemComparer = itemComparer;
			var selected = SelectedTreeNode;
			Sort();
			SelectedTreeNode = selected;
		}
	}
}
