namespace NutriNET.Api.Dto.User
{
    public record ResetPasswordRequest(string Email, string Code, string NewPassword);
}
