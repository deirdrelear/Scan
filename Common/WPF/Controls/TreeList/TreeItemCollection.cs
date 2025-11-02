using System;

namespace Common.WPF.Controls.TreeList
{
	public class TreeItemCollection : ObservableCollectionAdv<TreeNodeViewModel>
	{
		TreeNodeViewModel _owner;

		public TreeItemCollection(TreeNodeViewModel owner)
		{
			_owner = owner;
		}

		protected override void ClearItems()
		{
			while (Count != 0)
				RemoveAt(Count - 1);
		}
		protected override void InsertItem(int index, TreeNodeViewModel item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (item.ParentNode != _owner)
			{
				if (item.ParentNode != null)
					item.ParentNode.Nodes.Remove(item);
				item.ParentNode = _owner;
				item.Index = index;
				if (_owner._sortOrder != null)
					_owner._sortOrder.Add(index);
				//item._tree = _owner.Tree;
				for (int i = index; i < Count; i++)
					this[i].Index++;
				base.InsertItem(index, item);
			}
		}
		protected override void RemoveItem(int index)
		{
			TreeNodeViewModel item = this[index];
			if (_owner._sortOrder != null)
			{
				_owner._sortOrder.Remove(index);
				for (int i = 0; i < _owner._sortOrder.Count; i++)
					if (_owner._sortOrder[i] > index)
						_owner._sortOrder[i]--;
			}
			item.Index = -1;
			for (int i = index + 1; i < Count; i++)
				this[i].Index--;
			base.RemoveItem(index);
			item.ParentNode = null;
			item._root = null;
		}
		protected override void SetItem(int index, TreeNodeViewModel item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));
			RemoveAt(index);
			InsertItem(index, item);
		}
	}
}