using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ApiClients
{
    public record RequestResult(
        bool Success,
        string? Error
    );
}
