using System.Collections.Generic;
using System.Data;
using System.Drawing.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Helpers
{
    public static class MyExtensions
    {
        public static string ValidateDouble(this string text)
        {
            Regex regex = new Regex(@"\s*[a-z]*[A-Z]*");
            text = regex.Replace(text, "");
            text = text.Replace(",", ".");
            return text;
        }

        public static string ValidateInt(this string text)
        {
            Regex regex = new Regex(@"\s*[a-z]*[A-Z]*");
            text = regex.Replace(text, "");
            text = text.Replace(",", "");
            text = text.Replace(".", "");
            return text;
        }
    }
}
