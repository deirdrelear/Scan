using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Common.Helpers
{
	public static class BringToFront
	{
		[DllImport("USER32.DLL")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

		// Delegate to filter which windows to include 
		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		/// <summary> Get the text for the window pointed to by hWnd </summary>
		public static string GetWindowText(IntPtr hWnd)
		{
			int size = GetWindowTextLength(hWnd);
			if (size > 0)
			{
				var builder = new StringBuilder(size + 1);
				GetWindowText(hWnd, builder, builder.Capacity);
				return builder.ToString();
			}
			return string.Empty;
		}

		/// <summary> Find all windows that match the given filter </summary>
		/// <param name="filter"> A delegate that returns true for windows
		///    that should be returned and false for windows that should
		///    not be returned </param>
		public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
		{
			List<IntPtr> windows = new List<IntPtr>();

			EnumWindows(delegate (IntPtr wnd, IntPtr param)
			{
				if (filter(wnd, param))
				{
					// only add the windows that pass the filter
					windows.Add(wnd);
				}
				// but return true here so that we iterate all windows
				return true;
			}, IntPtr.Zero);
			return windows;
		}

		/// <summary> Find all windows that contain the given title text </summary>
		/// <param name="titleText"> The text that the window title must contain. </param>
		public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
		{
			return FindWindows((wnd, param) => GetWindowText(wnd).Contains(titleText));
		}
		public static void ToFront(string title)
		{
			var windowIds = FindWindowsWithText(title);
			foreach (IntPtr windowId in windowIds)
			{
				SetForegroundWindow(windowId);

			}
		}
	}
}
