namespace NutriNET.Data.Models
{
    public class FoodRequest 
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Length(8,13)]
        public string Barcode { get; set; }

        [Required]
        public string Brand { get; set; }

        public RequestStatus Status { get; set; }
        public DateTime DateSent { get; set; }
        public DateTime? ActionedOn { get; set; }

        public int? SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        public int? ActionedById { get; set; }
        [ForeignKey(nameof(ActionedById))]
        public User ActionedBy { get; set; }

        public FoodRequest() { }

        public FoodRequest(string name, string barcode, string brand, int senderId)
        {
            Name = name;
            Barcode = barcode;
            Brand = brand;
            SenderId = senderId;
        }

        public FoodRequest(int id, RequestStatus status, int actionedById)
        {
            Id = id;
            Status = status;
            ActionedById = actionedById;
        }
    }
}
