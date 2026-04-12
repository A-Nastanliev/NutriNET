using NutriNET.Maui.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public sealed class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value || value is not DateTime utcDateTime)
            {
                if (parameter is string str)
                    return (LocalizationResourceManager.Instance[str].ToString());

                return string.Empty;
            }

            if (utcDateTime.Kind == DateTimeKind.Unspecified)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            var localTime = utcDateTime.ToLocalTime();

            return localTime.ToString("HH:mm d MMMM yyyy", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
