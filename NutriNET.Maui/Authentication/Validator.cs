using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.Authentication
{
    public static class Validator
    {
        public static bool IsEmailValid(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPasswordValid(string password)
        {
            if (password.Length < 8 || password.Length > 64) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            return true;
        }
    }
}
