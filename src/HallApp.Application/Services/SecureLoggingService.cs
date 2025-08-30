using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services
{
    /// <summary>
    /// Service to handle logging securely without exposing sensitive information
    /// </summary>
    public class SecureLoggingService
    {
        private readonly ILogger<SecureLoggingService> _logger;

        // Patterns to identify sensitive data
        private static readonly Regex _creditCardPattern = new Regex(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);
        private static readonly Regex _ssnPattern = new Regex(@"\b\d{3}[-]?\d{2}[-]?\d{4}\b", RegexOptions.Compiled);
        private static readonly Regex _emailPattern = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex _passwordPattern = new Regex(@"(?i)(password|passwd|pwd)[\s:=]+[^\s]+", RegexOptions.Compiled);
        private static readonly Regex _jwtPattern = new Regex(@"eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*", RegexOptions.Compiled);

        public SecureLoggingService(ILogger<SecureLoggingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Log information with sensitive data redaction
        /// </summary>
        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(RedactSensitiveData(message), args);
        }

        /// <summary>
        /// Log warning with sensitive data redaction
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(RedactSensitiveData(message), args);
        }

        /// <summary>
        /// Log error with sensitive data redaction
        /// </summary>
        public void LogError(Exception exception, string message, params object[] args)
        {
            // Redact sensitive data from the exception details too
            var sanitizedMessage = RedactSensitiveData(message);
            var sanitizedExceptionMessage = exception != null ? 
                                           RedactSensitiveData(exception.Message) : 
                                           string.Empty;
            
            if (exception != null && exception.Message != sanitizedExceptionMessage)
            {
                // Create a new exception with sanitized message if sensitive data was found
                var sanitizedException = new Exception(sanitizedExceptionMessage, exception);
                _logger.LogError(sanitizedException, sanitizedMessage, args);
            }
            else
            {
                _logger.LogError(exception, sanitizedMessage, args);
            }
        }

        /// <summary>
        /// Redacts sensitive information like credit cards, SSNs, emails, passwords from logs
        /// </summary>
        private string RedactSensitiveData(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Redact credit card numbers
            input = _creditCardPattern.Replace(input, "[REDACTED CARD]");
            
            // Redact SSNs
            input = _ssnPattern.Replace(input, "[REDACTED SSN]");
            
            // Redact emails
            input = _emailPattern.Replace(input, "[REDACTED EMAIL]");
            
            // Redact passwords
            input = _passwordPattern.Replace(input, "$1: [REDACTED]");
            
            // Redact JWT tokens
            input = _jwtPattern.Replace(input, "[REDACTED JWT]");
            
            return input;
        }

        /// <summary>
        /// Log security-related events separately for audit purposes
        /// </summary>
        public void LogSecurityEvent(string eventType, string description, string username = null, string ipAddress = null)
        {
            var logMessage = $"SECURITY EVENT: {eventType} - {description}";
            
            if (!string.IsNullOrEmpty(username))
            {
                logMessage += $" - User: {username}";
            }
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                logMessage += $" - IP: {ipAddress}";
            }
            
            _logger.LogWarning(logMessage);
        }
    }
}
