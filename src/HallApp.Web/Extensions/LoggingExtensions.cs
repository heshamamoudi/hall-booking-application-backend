using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HallApp.Web.Extensions
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Adds secure logging configuration using Serilog
        /// Implements OWASP A09:2021 - Security Logging and Monitoring Failures
        /// </summary>
        public static IHostBuilder AddSecureLogging(this IHostBuilder builder)
        {
            return builder.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: 
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
            }, writeToProviders: false);
        }
        
        /// <summary>
        /// Creates filters for sensitive data using Serilog expressions
        /// </summary>
        private static Func<LogEvent, bool> SensitiveDataFilters()
        {
            // Using a simple lambda function to detect sensitive data in logs
            return evt => 
            {
                if (evt.MessageTemplate.Text == null) return false;
                
                // JWT Token pattern check
                if (evt.MessageTemplate.Text.Contains("eyJ") && 
                    evt.MessageTemplate.Text.Contains(".")) return true;
                
                // Password content check for non-warning logs
                if (evt.Level < LogEventLevel.Warning && 
                    (evt.MessageTemplate.Text.Contains("password") || 
                     evt.MessageTemplate.Text.Contains("Password"))) return true;
                
                return false;
            };
        }
        
        /// <summary>
        /// Destructuring policy to redact sensitive data in structured logs
        /// </summary>
        internal class SecureDataDestructuringPolicy : Serilog.Core.IDestructuringPolicy
        {
            private readonly Dictionary<string, Regex> _patterns = new Dictionary<string, Regex>
            {
                ["Password"] = new Regex(@"password|pwd", RegexOptions.IgnoreCase),
                ["CreditCard"] = new Regex(@"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|6(?:011|5[0-9]{2})[0-9]{12})\b"),
                ["SSN"] = new Regex(@"\b\d{3}-\d{2}-\d{4}\b"),
                ["Email"] = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b"),
                ["JWT"] = new Regex(@"eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*")
            };
            
            public bool TryDestructure(object value, Serilog.Core.ILogEventPropertyValueFactory propertyValueFactory, out Serilog.Events.LogEventPropertyValue result)
            {
                if (value is string stringValue)
                {
                    // Redact sensitive data
                    foreach (var pattern in _patterns)
                    {
                        stringValue = pattern.Value.Replace(stringValue, $"[REDACTED:{pattern.Key}]");
                    }
                    
                    result = propertyValueFactory.CreatePropertyValue(stringValue, true);
                    return true;
                }
                
                result = null;
                return false;
            }
        }
    }
}
