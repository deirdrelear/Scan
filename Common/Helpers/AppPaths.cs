using System;
using System.IO;
using System.Xml.Linq;

namespace Common.Helpers
{
	public static class AppPaths
	{
		public static string ExeFolder { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		public static string ExeName { get; } = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
        public static string RCPath { get; } = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RubezhCad"), "RC.xml");       
        public static string ParentDirectory { get; } = Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)).ToString();

    }
}
