using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Settings;

namespace NutriNET.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(ResetCodePage), typeof(ResetCodePage));
        }
    }
}
