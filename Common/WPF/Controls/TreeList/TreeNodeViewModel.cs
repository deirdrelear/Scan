using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Common.WPF.Controls.TreeList
{
	public class TreeNodeViewModel : BaseViewModel, IDisposable
	{
		public List<int> _sortOrder;
		internal RootTreeNodeViewModel _root;
		RootTreeNodeViewModel Root
		{
			get
			{
				if (_root == null && ParentNode != null)
					_root = ParentNode.Root;
				return _root;
			}
		}
		public void AssignToRoot(RootTreeNodeViewModel root)
		{
			_root = root;
			foreach (var child in Nodes)
				child.AssignToRoot(root);
		}

		protected bool IsRoot { get; set; }
		public int Level
		{
			get
			{
				if (this == Root)
					return -1;
				if (ParentNode == Root)
					return 0;
				return ParentNode.Level + 1;
			}
		}
		public int Index { get; set; }
		public bool IsSorted
		{
			get { return _sortOrder != null; }
		}
		public int VisualIndex
		{
			get { return ParentNode?._sortOrder?.IndexOf(Index) ?? Index; }
		}
		public TreeNodeViewModel ParentNode { get; set; }
		public TreeItemCollection Nodes { get; private set; }

		protected bool InsertSorted
		{
			get { return true; }
		}

		internal TreeNodeViewModel()
		{
			IsRoot = false;
			_root = null;
			Index = -1;
			Nodes = new TreeItemCollection(this);
			Nodes.CollectionChanged += ChildrenChanged;
			Nodes.CollectionChanged += OnChildrenChanged;
		}

		bool IsVisible
		{
			get
			{
				TreeNodeViewModel node = ParentNode;
				while (node != null)
				{
					if (!node.IsExpanded)
						return false;
					node = node.ParentNode;
				}
				return true;
			}
		}
		TreeNodeViewModel BottomNode
		{
			get
			{
				if (ParentNode != null)
				{
					if (ParentNode.NextNode != null)
						return ParentNode.NextNode;
					return ParentNode.BottomNode;
				}
				return null;
			}
		}
		TreeNodeViewModel NextVisibleNode
		{
			get
			{
				if (IsExpanded && Nodes.Count > 0)
					return GetNodeByVisualIndex(0);
				return NextNode ?? BottomNode;
			}
		}
		TreeNodeViewModel NextNode
		{
			get
			{
				if (ParentNode != null)
				{
					int index = VisualIndex;
					if (index >= 0 && index < ParentNode.Nodes.Count - 1)
						return ParentNode.GetNodeByVisualIndex(index + 1);
				}
				return null;
			}
		}
		int VisibleChildrenCount
		{
			get { return AllVisibleChildren.Count(); }
		}
		IEnumerable<TreeNodeViewModel> AllVisibleChildren
		{
			get
			{
				int level = Level;
				TreeNodeViewModel node = this;
				while (true)
				{
					node = node.NextVisibleNode;
					if (node != null && node.Level > level)
						yield return node;
					else
						break;
				}
			}
		}

		protected void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
		}
		void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (IsExpanded && Root != null)
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						if (e.NewItems != null)
						{
							if (InsertSorted && Root?.ItemComparer != null)
								InnerSort();
							int newVisualIndex = InsertSorted && _sortOrder == null ? e.NewStartingIndex : _sortOrder.IndexOf(e.NewStartingIndex);
							int index;
							if (newVisualIndex == 0)
								index = Root.Rows.IndexOf(this);
							else
							{
								var previosIndex = _sortOrder?[newVisualIndex - 1] ?? newVisualIndex - 1;
								index = Root.Rows.IndexOf(Nodes[previosIndex]) + Nodes[previosIndex].VisibleChildrenCount;
							}
							foreach (TreeNodeViewModel node in e.NewItems)
							{
								Root.Rows.Insert(index + 1, node);
								node.CreateChildrenRows();
								index++;
							}
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						foreach (TreeNodeViewModel node in e.OldItems)
							node.DropChildrenRows(true);
						break;
					case NotifyCollectionChangedAction.Move:
					case NotifyCollectionChangedAction.Replace:
					case NotifyCollectionChangedAction.Reset:
						DropChildrenRows(false);
						CreateChildrenRows();
						break;
				}
			OnPropertyChanged(() => HasChildren);
			OnPropertyChanged(() => Nodes);
		}
		void DropChildrenRows(bool removeParent)
		{
			int start = Root.Rows.IndexOf(this);
			if (start >= 0 || IsRoot)
			{
				int count = VisibleChildrenCount;
				if (removeParent)
					count++;
				else
					start++;
				Root.Rows.RemoveRange(start, count);
			}
		}
		void CreateChildrenRows()
		{
			int index = Root.Rows.IndexOf(this);
			if (index >= 0 || IsRoot)
			{
				var nodes = AllVisibleChildren.ToArray();
				Root.Rows.InsertRange(index + 1, nodes);
				//if (nodes.Contains(Tree.SelectedTreeItem))
				//	Tree.ResumeSelection();
			}
		}

		bool _isExpanded;
		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (value != IsExpanded)
				{
					if (value)
					{
						_isExpanded = true;
						if (Root != null)
						{
							CreateChildrenRows();
							Root.ResumeSelection();
						}
					}
					else
					{
						if (Root != null)
						{
							Root.SuspendSelection();
							DropChildrenRows(false);
						}
						_isExpanded = false;
					}
					OnPropertyChanged(() => HasChildren);
					OnPropertyChanged(() => IsExpanded);
				}
			}
		}

		public bool IsSelected
		{
			get { return Root != null && Root.SelectedTreeNode == this; }
			set
			{
				if (Root != null)
				{
					ExpandToThis();
					Root.SelectedTreeNode = this;
				}
				OnPropertyChanged(() => IsSelected);
			}
		}
		public bool HasChildren
		{
			get { return Nodes.Count > 0; }
		}

		public void ExpandToThis()
		{
			var parent = ParentNode;
			while (parent != null)
			{
				parent.IsExpanded = true;
				parent = parent.ParentNode;
			}
		}
		public void CollapseChildren(bool withSelf = true)
		{
			ProcessAllChildren(this, withSelf, item => item.IsExpanded = false);
		}
		public void ExpandChildren(bool withSelf = true)
		{
			ProcessAllChildren(this, withSelf, item => item.IsExpanded = true);
		}
		void ProcessAllChildren(TreeNodeViewModel parent, bool withSelf, Action<TreeNodeViewModel> action)
		{
			if (withSelf)
				action(parent);
			foreach (TreeNodeViewModel t in parent.Nodes)
				ProcessAllChildren(t, true, action);
		}

		protected void Sort()
		{
			if (Root?.ItemComparer != null)
			{
				DropChildrenRows(false);
				InnerSort();
				CreateChildrenRows();
			}
		}
		protected void InnerSort()
		{
			_sortOrder = new List<int>();
			if (Nodes.Count > 0)
			{
				var list = Nodes.ToList();
				QSort.Sort(list, Root.ItemComparer.Compare, Root.SortDirection != ListSortDirection.Ascending);
				foreach (TreeNodeViewModel t in list)
				{
					t.InnerSort();
					_sortOrder.Add(t.Index);
				}
			}
		}

		protected TreeNodeViewModel GetNodeByVisualIndex(int index)
		{
			return IsSorted ? Nodes[_sortOrder[index]] : Nodes[index];
		}


		#region IDisposable Members

		public void Dispose()
		{
			if (Nodes != null)
			{
				Nodes.CollectionChanged -= ChildrenChanged;
				Nodes = null;
			}
		}

		#endregion
	}

	public class TreeNodeViewModel<T> : TreeNodeViewModel
		where T : TreeNodeViewModel<T>
	{
		protected TreeNodeViewModel()
		{
		}
		public TreeNodeViewModel(IEnumerable<T> children)
			: this()
		{
			Nodes.InsertRange(0, (TreeItemCollection)children);
		}

		public T Parent
		{
			get { return ParentNode as T; }
		}
		public T this[int index]
		{
			get { return Nodes[index] as T; }
		}
		public void AddChild(T item)
		{
			Nodes.Add(item);
		}
		public void AddChildFirst(T item)
		{
			Nodes.Insert(0, item);
		}
		public void InsertChild(T item)
		{
			var index = Parent.Nodes.IndexOf(this);
			Parent.Nodes.Insert(index + 1, item);
		}

		public void InsertTo(T item, bool isAfterItem)
		{
			var index = Parent.Nodes.IndexOf(this);
			Parent.Nodes.Insert(index + (isAfterItem? 1: 0), item);
		}
		public void RemoveChild(T item)
		{
			Nodes.Remove(item);
		}
		public void ClearChildren()
		{
			Nodes.Clear();
		}
		public int ChildrenCount
		{
			get { return Nodes.Count; }
		}
		public IEnumerable<T> Children
		{
			get
			{
				return Nodes.Cast<T>();
			}
		}
		public List<T> GetAllChildren(bool withSelf = true)
		{
			var list = new List<T>();
			if (withSelf)
				list.Add((T)this);
			foreach (TreeNodeViewModel<T> child in Nodes)
				list.AddRange(child.GetAllChildren());
			return list;
		}
		public List<T> GetAllParents()
		{
			if (Parent == null)
				return new List<T>();
			else
			{
				List<T> allParents = Parent.GetAllParents();
				allParents.Add(Parent);
				return allParents;
			}
		}
		public T GetChildByVisualIndex(int index)
		{
			return GetNodeByVisualIndex(index) as T;
		}
	}
}