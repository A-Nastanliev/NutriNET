namespace NutriNET.Data.Models
{
    public class Meal : INutritionalValue
    {
        [Key]
        public int Id { get; set; }

        public MealType Type { get; set; }

        public DateTime DateTime { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Required]
        public List<MealFood> MealFoods { get; set; }

        public Meal() { }

        public Meal(int userId, MealType type)
        {
            UserId = userId;
            Type = type;
        }

        public double GetCalories()
        {
            double total = 0;
            foreach (MealFood mf in MealFoods) 
            {
                total += mf.GetCalories();
            }
            return total;
        }

        public double GetCarbohydrates()
        {
            double total = 0;
            foreach (MealFood mf in MealFoods)
            {
                total += mf.GetCarbohydrates();
            }
            return total;
        }

        public double GetFats()
        {
            double total = 0;
            foreach (MealFood mf in MealFoods)
            {
                total += mf.GetFats();
            }
            return total;
        }

        public double GetProteins()
        {
            double total = 0;
            foreach (MealFood mf in MealFoods)
            {
                total += mf.GetProteins();
            }
            return total;
        }
    }
}
