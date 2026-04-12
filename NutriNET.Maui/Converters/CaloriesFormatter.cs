using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public class CaloriesFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                double rounded = d >= 1 ? Math.Round(d, 1) : Math.Round(d, 2);
                return $"{rounded:G} kcal";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
