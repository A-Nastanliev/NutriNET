using NutriNET.Maui.Authentication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NutriNET.Maui.Converters
{
    public class RoleVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is not UserRole role || parameter == null)
                return false;

            IEnumerable<UserRole> allowedRoles = parameter switch
            { 
                UserRole singleRole => new[] { singleRole },

                string rolesParam => rolesParam
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(r => Enum.TryParse<UserRole>(r, out var parsed) ? parsed : (UserRole?)null)
                    .Where(r => r.HasValue)
                    .Select(r => r!.Value),

                _ => Enumerable.Empty<UserRole>()
            };

            return allowedRoles.Contains(role);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }    
}
