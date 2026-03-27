using NutriNET.Data.Enums;
using Org.BouncyCastle.Asn1.Mozilla;
using System.ComponentModel.DataAnnotations;
using NutriNET.Api.Dto.User;

namespace NutriNET.Api.Dto.Food
{
    public class FoodRequestDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Brand { get; set; }

        public string Barcode { get; set; }

        public DateTime DateSent { get; set; }
        public DateTime? ActionDate { get; set; }

        public RequestStatus Status { get; set; }

        public PublicUserDto? Sender { get; set; }

        public PublicUserDto? ActionUser { get; set; }

        public FoodRequestDto() { }

        public FoodRequestDto(int id,string name, string brand, string barcode,DateTime dateSent, DateTime? actionDate,
            RequestStatus status, PublicUserDto actionUser , PublicUserDto sender)
        {
            Id = id;
            Name = name;
            Brand = brand;
            Barcode = barcode;
            Status = status;
            ActionUser = actionUser;
            Sender = sender;
            DateSent = dateSent;
            ActionDate = actionDate;
        }
    }
}
