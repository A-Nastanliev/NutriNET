using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public class EqualsConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = object.Equals(value, parameter);
            return Invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}