namespace Common.WPF.Controls.TreeList
{
	public interface IItemComparer
	{
		int Compare(TreeNodeViewModel x, TreeNodeViewModel y);
	}
}