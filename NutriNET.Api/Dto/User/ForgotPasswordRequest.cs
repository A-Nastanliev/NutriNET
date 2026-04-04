namespace NutriNET.Api.Dto.User
{
    public record ForgotPasswordRequest(string Email, string Language = "en-US");
}
