using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (Math.Abs(d) < double.Epsilon)
                    return string.Empty;

                return d.ToString(CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Replace(',', '.');

            if (string.IsNullOrWhiteSpace(text))
                return 0d;

            if (text.EndsWith("."))
                return Binding.DoNothing;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return Binding.DoNothing;
        }
    }

}
