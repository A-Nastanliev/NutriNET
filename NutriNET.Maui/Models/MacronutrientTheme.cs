using CommunityToolkit.Mvvm.ComponentModel;

namespace NutriNET.Maui.Models
{
    public partial class MacronutrientTheme : ObservableObject
    {
        [ObservableProperty]
        Color protein;

        [ObservableProperty]
        Color carbs;

        [ObservableProperty]
        Color fat;

        public string Name;

        public ResourceDictionary Theme;

        public MacronutrientTheme(string name,ResourceDictionary resources)
        {
            Protein = (Color)resources["ProteinColor"];
            Carbs = (Color)resources["CarbsColor"];
            Fat = (Color)resources["FatColor"];
            Name = name;
            Theme = resources;
        }
    }
}
