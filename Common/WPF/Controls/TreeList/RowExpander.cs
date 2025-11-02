using System.Windows;
using System.Windows.Controls;

namespace Common.WPF.Controls.TreeList
{
	public class RowExpander : Control
	{
		static RowExpander()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RowExpander), new FrameworkPropertyMetadata(typeof(RowExpander)));
		}
	}
}
