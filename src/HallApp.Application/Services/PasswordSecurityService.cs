using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HallApp.Application.Services
{
    /// <summary>
    /// Service to enforce secure password policies according to OWASP guidelines
    /// </summary>
    public class PasswordSecurityService
    {
        private readonly ILogger<PasswordSecurityService> _logger;

        public PasswordSecurityService(ILogger<PasswordSecurityService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates password strength according to OWASP guidelines
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if password meets requirements, otherwise false</returns>
        public bool ValidatePasswordStrength(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Password cannot be empty";
                return false;
            }

            // Check minimum length (OWASP recommends at least 8 characters)
            if (password.Length < 8)
            {
                errorMessage = "Password must be at least 8 characters long";
                return false;
            }

            // Check complexity requirements
            bool hasUppercase = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

            if (!hasUppercase || !hasLowercase || !hasDigit || !hasSpecialChar)
            {
                errorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character";
                return false;
            }

            // Check for common passwords (this would typically use a more comprehensive list)
            string[] commonPasswords = { "Password1!", "Admin123!", "Welcome1!", "123456789A!", "Qwerty123!" };
            if (commonPasswords.Contains(password))
            {
                errorMessage = "Password is too common. Please choose a more unique password";
                return false;
            }

            // Check for repetitive or sequential characters
            if (HasRepeatingPatterns(password))
            {
                errorMessage = "Password contains repetitive or sequential patterns";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for repeating or sequential patterns in a password
        /// </summary>
        private bool HasRepeatingPatterns(string password)
        {
            // Check for 3+ repeating characters
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                {
                    return true;
                }
            }

            // Check for sequential characters like "abc", "123"
            string sequences = "abcdefghijklmnopqrstuvwxyz0123456789";
            for (int i = 0; i < sequences.Length - 3; i++)
            {
                string forward = sequences.Substring(i, 3);
                string backward = new string(forward.Reverse().ToArray());

                if (password.ToLower().Contains(forward) || password.ToLower().Contains(backward))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a cryptographically secure random password
        /// </summary>
        /// <param name="length">Password length</param>
        /// <param name="includeSpecialChars">Whether to include special characters</param>
        /// <returns>A secure random password</returns>
        public string GenerateSecurePassword(int length = 16, bool includeSpecialChars = true)
        {
            if (length < 8)
            {
                throw new ArgumentException("Password length must be at least 8 characters", nameof(length));
            }

            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            string availableChars = lowerChars + upperChars + numberChars;
            if (includeSpecialChars)
            {
                availableChars += specialChars;
            }

            // Use cryptographically secure random number generator
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            char[] password = new char[length];
            
            // Ensure at least one of each required character type
            password[0] = lowerChars[randomBytes[0] % lowerChars.Length];
            password[1] = upperChars[randomBytes[1] % upperChars.Length];
            password[2] = numberChars[randomBytes[2] % numberChars.Length];
            
            if (includeSpecialChars)
            {
                password[3] = specialChars[randomBytes[3] % specialChars.Length];
            }

            // Fill the rest with random characters
            for (int i = includeSpecialChars ? 4 : 3; i < length; i++)
            {
                password[i] = availableChars[randomBytes[i] % availableChars.Length];
            }

            // Shuffle the password
            ShuffleArray(password);
            
            return new string(password);
        }

        /// <summary>
        /// Shuffle an array using the Fisher-Yates algorithm with secure randomization
        /// </summary>
        private void ShuffleArray(char[] array)
        {
            using var rng = RandomNumberGenerator.Create();
            
            for (int i = array.Length - 1; i > 0; i--)
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int randomIndex = BitConverter.ToInt32(randomBytes, 0) % (i + 1);
                if (randomIndex < 0) randomIndex += (i + 1);
                
                char temp = array[i];
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }
    }
}
