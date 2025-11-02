using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Common.WPF.Controls.TreeList
{
	public class TreeList : ListView, ITreeList
	{
		public static DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(IEnumerable), typeof(TreeList), new FrameworkPropertyMetadata(null, OnSourceChanged));
		public IEnumerable Source
		{
			get { return (IEnumerable)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}
		static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var treeList = (TreeList)d;
			treeList.Root.SetSource((IEnumerable)e.NewValue);
		}

		public static DependencyProperty RootProperty = DependencyProperty.Register("Root", typeof(RootTreeNodeViewModel), typeof(TreeList), new FrameworkPropertyMetadata(null, OnRootChanged));
		public RootTreeNodeViewModel Root
		{
			get { return (RootTreeNodeViewModel)GetValue(RootProperty); }
			set { SetValue(RootProperty, value); }
		}
		static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var treeList = (TreeList)d;
			((RootTreeNodeViewModel)e.OldValue)?.DisassignFromTree(treeList);
			((RootTreeNodeViewModel)e.NewValue).AssignToTree(treeList);
			treeList.ItemsSource = ((RootTreeNodeViewModel)e.NewValue).Rows;
		}

		public static DependencyProperty SelectedTreeNodeProperty = DependencyProperty.Register("SelectedTreeNode", typeof(object), typeof(TreeList), new FrameworkPropertyMetadata(null, OnSelectedTreeItemChanged) { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
		public object SelectedTreeNode
		{
			get { return GetValue(SelectedTreeNodeProperty); }
			set { SetValue(SelectedTreeNodeProperty, value); }
		}
		static void OnSelectedTreeItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var treeList = (TreeList)d;
			treeList.SelectedItem = e.NewValue;
			treeList.ScrollIntoView(treeList.SelectedTreeNode);
			treeList.Root.SelectedTreeNode = e.NewValue;
		}

		public static DependencyProperty ItemActivatedCommandProperty = DependencyProperty.Register("ItemActivatedCommand", typeof(ICommand), typeof(TreeList));
		public ICommand ItemActivatedCommand
		{
			get { return (ICommand)GetValue(ItemActivatedCommandProperty); }
			set { SetValue(ItemActivatedCommandProperty, value); }
		}
		public static DependencyProperty ItemActivatedCommandParameterProperty = DependencyProperty.Register("ItemActivatedCommandParameter", typeof(object), typeof(TreeList));
		public object ItemActivatedCommandParameter
		{
			get { return GetValue(ItemActivatedCommandParameterProperty); }
			set { SetValue(ItemActivatedCommandParameterProperty, value); }
		}

		internal TreeNodeViewModel PendingFocusNode { get; set; }

		public TreeList()
		{
			Root = new RootTreeNodeViewModel();
			SelectionChanged += OnSelectionChanged;
			ItemContainerGenerator.StatusChanged += ItemContainerGeneratorStatusChanged;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new TreeListItem();
		}
		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is TreeListItem;
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			var ti = element as TreeListItem;
			var node = item as TreeNodeViewModel;
			if (ti != null && node != null)
			{
				ti.Node = node;
				ti.Tree = this;
				base.PrepareContainerForItemOverride(element, node);
			}
		}
		void ItemContainerGeneratorStatusChanged(object sender, EventArgs e)
		{
			if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && PendingFocusNode != null)
			{
				var item = ItemContainerGenerator.ContainerFromItem(PendingFocusNode) as TreeListItem;
				item?.Focus();
				PendingFocusNode = null;
			}
		}

		void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.OriginalSource == this && (e.AddedItems.Count > 0 || PendingFocusNode == null))
			{
				SelectedTreeNode = e.AddedItems.Count > 0 ? e.AddedItems[0] as TreeNodeViewModel : null;
				SetFocus(SelectedTreeNode);
			}
		}
		void SetFocus(object treeNode)
		{
			if (IsKeyboardFocused || IsKeyboardFocusWithin)
			{
				var item = ItemContainerGenerator.ContainerFromItem(treeNode) as TreeListItem;
				if (item != null)
					item.Focus();
				else
					PendingFocusNode = treeNode as TreeNodeViewModel;
			}
		}

		#region ITreeList Members

		ObservableCollectionAdv<TreeNodeViewModel> ITreeList.Rows => Root?.Rows;

		public void SuspendSelection()
		{
			PendingFocusNode = SelectedTreeNode as TreeNodeViewModel;
		}
		public void ResumeSelection()
		{
			SelectedItem = SelectedTreeNode;
		}

		#endregion
	}
}