using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services
{
    /// <summary>
    /// Service to protect against SQL injection attacks by validating input
    /// </summary>
    public class SqlInjectionProtectionService
    {
        private readonly ILogger<SqlInjectionProtectionService> _logger;
        private readonly Regex _sqlInjectionPattern;

        public SqlInjectionProtectionService(ILogger<SqlInjectionProtectionService> logger)
        {
            _logger = logger;
            
            // Comprehensive SQL injection pattern
            _sqlInjectionPattern = new Regex(
                @"(\b(select|insert|update|delete|from|drop|alter|create|where|union|join|exec|execute|executing|sp_|xp_|declare|waitfor|delay|cast|convert)\b)|(\b(table|database|sys|information_schema)\b)|('|%27|--|;|/\*|\*/|@@|\|\||@@version|char\(|nchar\()",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }

        /// <summary>
        /// Validates input against SQL injection patterns
        /// </summary>
        /// <param name="input">The string to validate</param>
        /// <returns>True if the input is safe, false if potential SQL injection is detected</returns>
        public bool ValidateInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true; // Empty input is not a SQL injection
            }

            bool isSafe = !_sqlInjectionPattern.IsMatch(input);
            
            if (!isSafe)
            {
                _logger.LogWarning("Potential SQL injection detected: {Input}", input);
            }
            
            return isSafe;
        }

        /// <summary>
        /// Parameterizes dynamic SQL queries to prevent SQL injection
        /// </summary>
        /// <param name="columnName">The column name to be used in a query</param>
        /// <returns>A safe column name or throws an exception</returns>
        public string ValidateColumnName(string columnName)
        {
            // Validate column name (should only contain alphanumeric and underscore)
            if (!Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$"))
            {
                _logger.LogWarning("Invalid column name detected: {ColumnName}", columnName);
                throw new ArgumentException("Invalid column name format");
            }
            
            return columnName;
        }

        /// <summary>
        /// Sanitize inputs for use in SQL queries
        /// </summary>
        /// <param name="input">The string to sanitize</param>
        /// <returns>Sanitized string safe for use in SQL</returns>
        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            
            // Replace potentially dangerous characters
            string sanitized = input
                .Replace("'", "''")
                .Replace("--", "")
                .Replace(";", "")
                .Replace("/*", "")
                .Replace("*/", "")
                .Replace("xp_", "")
                .Replace("sp_", "");
                
            return sanitized;
        }
        
        /// <summary>
        /// Creates a whitelist-based validator for table and column names
        /// </summary>
        /// <param name="allowedValues">List of allowed values</param>
        /// <returns>Function that validates against the whitelist</returns>
        public Func<string, bool> CreateWhitelistValidator(IEnumerable<string> allowedValues)
        {
            var whitelist = new HashSet<string>(allowedValues, StringComparer.OrdinalIgnoreCase);
            return (input) => whitelist.Contains(input);
        }
    }
}
