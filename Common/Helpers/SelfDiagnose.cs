using System.IO;
using System.Windows.Forms;

namespace Common.Helpers
{
	public static class SelfDiagnose
	{
		public static bool Exec()
		{
			if (File.Exists(Path.Combine(Path.Combine(AppPaths.ExeFolder, "Assets"), "Empty.dwg")) &&
				File.Exists(Path.Combine(Path.Combine(AppPaths.ExeFolder, "Assets"), "isocpeui.ttf")) &&
				File.Exists(Path.Combine(Path.Combine(AppPaths.ExeFolder, "Assets"), "isocpeur.ttf")) &&
				File.Exists(Path.Combine(Path.Combine(AppPaths.ExeFolder, "Assets"), "SpecificationTemplate.dotx")))
				return true;
            MessageBox.Show("Missing file(s) in folder 'Assets'.");
			return false;
		}
	}
}
