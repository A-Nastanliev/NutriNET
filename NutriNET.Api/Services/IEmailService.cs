namespace NutriNET.Api.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string code, string language = "en-US");
    }
}
