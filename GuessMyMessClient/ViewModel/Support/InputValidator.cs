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
        public const int MinPasswordLenght = 8;
        public const int MaxPasswordLenght = 25;
        public const int MinUsernameLenght = 5;



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

        // 2. NUEVO: Validar Nombre de Usuario
        public static bool IsValidUsername(string username, out string errorLangKey)
        {
            // 1. Validación inicial de nulos
            if (string.IsNullOrWhiteSpace(username))
            {
                errorLangKey = "alertUsernameEmpty";
                return false;
            }

            // 2. LIMPIEZA (.Trim): Elimina espacios al inicio y final
            // Es vital hacerlo ANTES de checar la longitud o el regex.
            username = username.Trim();

            // 3. Validar Longitud
            // (Asegúrate de usar las constantes que definimos arriba)
            if (username.Length < MinUsernameLenght)
            {
                errorLangKey = "alertUsernameTooShort";
                return false;
            }

            // 4. Regex Estricto:
            // ^           -> Inicio del texto
            // [a-zA-Z0-9] -> Solo letras (inglés, sin tildes) y números
            // +           -> Uno o más caracteres
            // $           -> Fin del texto
            // NO permite: Espacios intermedios, guiones, puntos, ni símbolos especiales.
            var regex = new Regex(@"^[a-zA-Z0-9]+$");

            if (!regex.IsMatch(username))
            {
                errorLangKey = "alertUsernameInvalidChars";
                return false;
            }

            errorLangKey = null;
            return true;
        }
        // 3. NUEVO: Validar Nombres Reales (Nombre y Apellido)
        public static bool IsValidName(string name)
        {
            // 1. Validación básica de nulos
            if (string.IsNullOrWhiteSpace(name)) return false;

            // 2. LIMPIEZA (.Trim): 
            // Esto borra los espacios al inicio y al final ("  Jose  " -> "Jose").
            // Es importante hacerlo ANTES de checar la longitud o el Regex.
            name = name.Trim();

            // Nota: Como ya hicimos Trim() arriba, no necesitamos preocuparnos 
            // por espacios al final en el regex.
            var regex = new Regex(@"^[\p{Lu}][\p{L}\s]*$");

            return regex.IsMatch(name);
        }


        public static bool IsPasswordSecure(string password, out string errorLangKey)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errorLangKey = "alertPasswordEmpty";
                return false;
            }

            if (password.Length < MinPasswordLenght)
            {
                errorLangKey = "alertPasswordTooShort";
                return false;
            }

            if (password.Length > MaxPasswordLenght)
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
