namespace NutriNET.Api.Dto.User
{
    public class UpdateEmailRequest
    {
        public string NewEmail { get; set; }
        public string CurrentPassword { get; set; }
    }
}
