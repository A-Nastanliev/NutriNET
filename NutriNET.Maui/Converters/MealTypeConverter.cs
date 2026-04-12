using NutriNET.Maui.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NutriNET.Maui.Models.Meal;

namespace NutriNET.Maui.Converters
{
    public class MealTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MealType mealType)
            {
                return mealType switch
                {
                    MealType.Snack => LocalizationResourceManager.Instance["Snack"],
                    MealType.Breakfast => LocalizationResourceManager.Instance["Breakfast"],
                    MealType.Lunch => LocalizationResourceManager.Instance["Lunch"],
                    MealType.Dinner => LocalizationResourceManager.Instance["Dinner"],
                    _ => value.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
