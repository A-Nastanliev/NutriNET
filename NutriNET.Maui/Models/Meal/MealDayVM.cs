using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.Models.Meal
{
    public partial class MealDayVM : ObservableObject, INutritionalValue, ILocalize
    {
        [ObservableProperty]
        DateTime date;  
        [ObservableProperty]
        ObservableCollection<MealVM> meals = new();

        public double Calories => Math.Round(GetCalories(), 2, MidpointRounding.AwayFromZero);
        public double Carbohydrates => Math.Round(GetCarbohydrates(), 2, MidpointRounding.AwayFromZero);
        public double Fats => Math.Round(GetFats(), 2, MidpointRounding.AwayFromZero);
        public double Proteins => Math.Round(GetProteins(), 2, MidpointRounding.AwayFromZero);

        public double CarbsRatio => Calories == 0 ? 0 : (Carbohydrates * 4 / Calories);
        public double ProteinRatio => Calories == 0 ? 0 : (Proteins * 4 / Calories);
        public double FatRatio => Calories == 0 ? 0 : (Fats * 9 / Calories);

        public MealDayVM() { }

        public MealDayVM(DateTime date, ObservableCollection<MealVM> meals)
        {
            Date = date;
            Meals= meals;
            RecalculateMacros();
        }

        public void RecalculateMacros()
        {
            OnPropertyChanged(nameof(Calories));
            OnPropertyChanged(nameof(Carbohydrates));
            OnPropertyChanged(nameof(Fats));
            OnPropertyChanged(nameof(Proteins));
            OnPropertyChanged(nameof(CarbsRatio));
            OnPropertyChanged(nameof(ProteinRatio));
            OnPropertyChanged(nameof(FatRatio));
        }

        public double GetCalories()
        {
            double total = 0;
            foreach (INutritionalValue m in Meals)
            {
                total += m.GetCalories();
            }
            return total;
        }

        public double GetCarbohydrates()
        {
            double total = 0;
            foreach (INutritionalValue m in Meals)
            {
                total += m.GetCarbohydrates();
            }
            return total;
        }

        public double GetFats()
        {
            double total = 0;
            foreach (INutritionalValue m in Meals)
            {
                total += m.GetFats();
            }
            return total;
        }

        public double GetProteins()
        {
            double total = 0;
            foreach (INutritionalValue m in Meals)
            {
                total += m.GetProteins();
            }
            return total;
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(Date));
            foreach (ILocalize m in Meals)
            {
                m.OnLocalize();
            }
        }
    }
}
