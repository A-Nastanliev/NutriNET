using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public class AllTrueMultiConverter : IMultiValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            bool allTrue = true;

            foreach (var value in values)
            {
                if (value is not bool boolValue || !boolValue)
                {
                    allTrue = false;
                    break;
                }
            }

            return Invert ? !allTrue : allTrue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
