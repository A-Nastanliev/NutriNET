namespace NutriNET.Maui.Models
{
    public interface INutritionalValue
    {
        double GetCalories();
        double GetProteins();          
        double GetCarbohydrates();        
        double GetFats();
        void RecalculateMacros();

    }
}
