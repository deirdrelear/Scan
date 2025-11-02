using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Common.Helpers
{
	public class IniFile
	{
		string _path;
		readonly string _exe = Path.Combine(AppPaths.ExeFolder, AppPaths.ExeName);

		[DllImport("kernel32")]
		static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

		[DllImport("kernel32")]
		static extern int GetPrivateProfileString(string section, string key, string Default, StringBuilder retVal, int size, string filePath);

		[DllImport("kernel32")]
		static extern int GetPrivateProfileString(string section, int key, string value, [MarshalAs(UnmanagedType.LPArray)] byte[] result, int size, string fileName);

		public IniFile(string iniPath = null)
		{
			_path = new FileInfo(iniPath ?? _exe + ".ini").FullName;
		}

		public string Read(string key, string section = null)
		{
			var retVal = new StringBuilder(255);
			GetPrivateProfileString(section ?? _exe, key, "", retVal, 255, _path);
			return retVal.ToString();
		}

		public void Write(string key, string value, string section = null)
		{
			WritePrivateProfileString(section ?? _exe, key, value, _path);
		}

		public bool KeyExists(string key, string section = null)
		{
			return Read(key, section).Length > 0;
		}

        public Dictionary<string, string> GetValueDictionary(int position, string section, bool isNumber)
        {
            var rezult = new Dictionary<string, string>();
            var keys = GetSectionKeys(section);
            double doubVal;
            int intVal;
            foreach (var key in keys)
            {
                var value = Read(key, section).Split(new[] {"###"}, StringSplitOptions.None);
                if (value.Length >= position && 
                    (!isNumber || (Double.TryParse(value[position], NumberStyles.Any, CultureInfo.InvariantCulture, out doubVal) || (int.TryParse(value[position], out intVal)))))
                    rezult.Add(key, value[position]);
            }
            return rezult;
        }

        public string[] GetSectionKeys(string section)
		{
			//    Sets the maxsize buffer to 500, if the more
			//    is required then doubles the size each time. 
			for (int maxsize = 500;; maxsize *= 2)
			{
				//    Obtains the EntryKey information in bytes
				//    and stores them in the maxsize buffer (Bytes array).
				//    Note that the SectionHeader value has been passed.
				byte[] bytes = new byte[maxsize];
				int size = GetPrivateProfileString(section, 0, "", bytes, maxsize, _path);

				// Check the information obtained is not bigger
				// than the allocated maxsize buffer - 2 bytes.
				// if it is, then skip over the next section
				// so that the maxsize buffer can be doubled.
				if (size < maxsize - 2)
				{
					// Converts the bytes value into an ASCII char.
					// This is one long string.
					string entries = Encoding.GetEncoding("Windows-1251").GetString(bytes, 0,
						size - (size > 0 ? 1 : 0));
					// Splits the Long string into an array based on the "\0"
					// or null (Newline) value and returns the value(s) in an array
					return entries.Split('\0');
				}
			}
		}
	}
}