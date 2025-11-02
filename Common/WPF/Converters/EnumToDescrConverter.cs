//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Windows.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;

namespace Common.WPF.Converters
{
    public class EnumToDescriptionConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";
            if (value.GetType().IsEnum)
                return ((Enum)value).ToDescription();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    public static class ExtensionMethods
    {
        public static bool IsNotNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection != null && collection.Count > 0;
        }

        public static bool IsExactly<T>(this object obj)
        {
            return obj != null && obj.GetType() == typeof(T);
        }

        public static string ToDescription(this Enum value)
        {
#if NET4
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
#else
            FieldInfo fieldInfo = value.GetType().GetTypeInfo().GetField(value.ToString());
#endif
            if (fieldInfo != null)
            {
                DescriptionAttribute[] descriptionAttributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptionAttributes.Length > 0)
                    return descriptionAttributes[0].Description;
                return value.ToString();
            }
            return null;
        }
    }
}
