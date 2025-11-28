using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class InputValidator
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s\.]{2,}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                return regex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsPasswordSecure(string password, out string errorLangKey)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errorLangKey = "alertPasswordEmpty";
                return false;
            }

            if (password.Length < 8)
            {
                errorLangKey = "alertPasswordTooShort";
                return false;
            }

            if (password.Length > 25)
            {
                errorLangKey = "alertPasswordTooLong";
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                errorLangKey = "alertPasswordNeedsUpper";
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                errorLangKey = "alertPasswordNeedsLower";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                errorLangKey = "alertPasswordNeedsDigit";
                return false;
            }

            if (password.All(char.IsLetterOrDigit))
            {
                errorLangKey = "alertPasswordNeedsSpecial";
                return false;
            }
            if (!password.Contains(","))
            {
                errorLangKey = "alertPasswordNeedsComma";
                return false;

            }

            errorLangKey = null;
            return true;
        }
    }
}
