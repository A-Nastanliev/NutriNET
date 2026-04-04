using Resend;

namespace NutriNET.Api.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly ResendClient _client;
        private readonly IConfiguration _config;

        public ResendEmailService(ResendClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string code, string language = "en-US")
        {
            var (subject, body) = GetLocalizedContent(code, language);

            var message = new EmailMessage
            {
                From = $"{_config["Resend:SenderName"]} <{_config["Resend:SenderEmail"]}>",
                To = { toEmail },
                Subject = subject,
                HtmlBody = body
            };

            await _client.EmailSendAsync(message);
        }

        private static (string Subject, string Body) GetLocalizedContent(string code, string language)
        {
            return language switch
            {
                "bg-BG" => (
                    Subject: "Вашият код за промяна на парола",
                    Body: $"""
                        <h2>Промяна на парола</h2>
                        <p>Вашият код е: <strong style="font-size:24px;letter-spacing:4px">{code}</strong></p>
                        <p>Изтича след <strong>15 минути</strong>. Ако не сте поискали това, игнорирайте имейла.</p>
                        """),
                _ => (
                    Subject: "Your Password Reset Code",
                    Body: $"""
                        <h2>Password Reset</h2>
                        <p>Your code is: <strong style="font-size:24px;letter-spacing:4px">{code}</strong></p>
                        <p>Expires in <strong>15 minutes</strong>. If you didn't request this, ignore this email.</p>
                        """)
            };
        }
    }
}
