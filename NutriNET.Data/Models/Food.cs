namespace NutriNET.Data.Models
{
    [Index(nameof(Barcode), IsUnique = true)]
    public class Food : INutritionalValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(30)]
        public string? Brand { get; set; }

        [Length(8, 14)]
        public string? Barcode { get; set; }

        public string? Image { get; set; }

        [Range(0, 900)]
        public double Calories { get; set; }

        [Range(0, 100)]
        public double Proteins { get; set; }

        [Range(0, 100)]
        public double Carbohydrates { get; set; }

        [Range(0, 100)]
        public double Fats { get; set; }

        public DateTime DateAdded { get; set; }

        public Food() { }

        public Food(string name, string brand, string barcode, string image, double calories, double proteins, double carbohydrates, double fats )
        {
            Name = name;
            Brand = brand;
            Barcode = barcode;
            Image = image;
            Calories = calories;
            Proteins = proteins;
            Carbohydrates = carbohydrates;
            Fats = fats;
        }

        public Food(int id, string name, string brand, string barcode, double calories, double proteins, double carbohydrates, double fats)
        {
            Id = id;
            Name = name;
            Brand = brand;
            Barcode = barcode;
            Calories = calories;
            Proteins = proteins;
            Carbohydrates = carbohydrates;
            Fats = fats;
        }

        public double GetCalories()
        {
            return Calories;
        }

        public double GetCarbohydrates()
        {
            return Carbohydrates;
        }

        public double GetFats()
        {
            return Fats;
        }

        public double GetProteins()
        {
            return Proteins;
        }
    }
}
