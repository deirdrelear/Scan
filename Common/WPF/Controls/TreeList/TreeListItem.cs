using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Common.WPF.Controls.TreeList
{
	public class TreeListItem : ListViewItem, INotifyPropertyChanged
	{
		TreeNodeViewModel _node;
		public TreeNodeViewModel Node
		{
			get { return _node; }
			internal set
			{
				_node = value;
				OnPropertyChanged("Node");
			}
		}

		static TreeListItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListItem), new FrameworkPropertyMetadata(typeof(TreeListItem)));
		}

		public TreeList Tree { get; internal set; }

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.Source.GetType() != typeof(RowExpander) && Node != null && Node.HasChildren)
				Node.IsExpanded = !Node.IsExpanded;
			if (Tree != null)
			{
				if (Tree.ItemActivatedCommand != null && Tree.ItemActivatedCommand.CanExecute(Tree.ItemActivatedCommandParameter))
					Tree.ItemActivatedCommand.Execute(Tree.ItemActivatedCommandParameter);
				else
				{
					var viewModel = Tree.DataContext as IEditingBaseViewModel;
					if (viewModel?.EditCommand != null && viewModel.EditCommand.CanExecute(null))
						viewModel.EditCommand.Execute();
				}
			}
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Node != null)
				switch (e.Key)
				{
					case Key.Right:
						e.Handled = true;
						if (!Node.IsExpanded)
						{
							Node.IsExpanded = true;
							ChangeFocus(Node);
						}
						else if (Node.Nodes.Count > 0)
							ChangeFocus(Node.Nodes[0]);
						break;
					case Key.Left:
						e.Handled = true;
						if (Node.IsExpanded && Node.HasChildren)
						{
							Node.IsExpanded = false;
							ChangeFocus(Node);
						}
						else
							ChangeFocus(Node.ParentNode);
						break;
					case Key.Subtract:
						e.Handled = true;
						Node.IsExpanded = false;
						ChangeFocus(Node);
						break;
					case Key.Add:
						e.Handled = true;
						Node.IsExpanded = true;
						ChangeFocus(Node);
						break;
				}
			if (!e.Handled)
				base.OnKeyDown(e);
		}

		void ChangeFocus(TreeNodeViewModel node)
		{
			if (Tree != null)
			{
				Tree.ScrollIntoView(node);
				var item = Tree.ItemContainerGenerator.ContainerFromItem(node) as TreeListItem;
				if (item != null)
					item.Focus();
				else
					Tree.PendingFocusNode = node;
			}
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
		#endregion
	}
}