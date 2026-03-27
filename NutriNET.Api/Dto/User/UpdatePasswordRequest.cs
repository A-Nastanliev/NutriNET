namespace NutriNET.Api.Dto.User
{
    public class UpdatePasswordRequest
    {
        public string NewPassword { get; set; }
        public string CurrentPassword { get; set; }
    }
}
