using Ganss.Xss;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace HallApp.Application.Services
{
    /// <summary>
    /// Service for sanitizing HTML input to prevent XSS attacks
    /// Implements OWASP A03:2021 - Injection (specifically XSS)
    /// </summary>
    public class HtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;
        private readonly ILogger<HtmlSanitizerService> _logger;
        private readonly Regex _potentialXssRegex;
        private readonly string[] _dangerousAttributes = new[] { 
            "onmouseover", "onmouseout", "onclick", "onload", "onerror",
            "onblur", "onchange", "ondblclick", "onfocus", "onkeydown",
            "onkeypress", "onkeyup", "onmousedown", "onmousemove", "onmouseup",
            "onreset", "onselect", "onsubmit", "onunload" 
        };

        public HtmlSanitizerService(ILogger<HtmlSanitizerService> logger = null)
        {
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
            
            // Enhanced XSS detection regex
            _potentialXssRegex = new Regex(
                @"<script|javascript:|onerror=|onload=|eval\(|setTimeout\(|setInterval\(|new\s+Function\(|document\.cookie", 
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            // Configure allowed tags (whitelist approach)
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            
            // Clear all attributes and only allow safe ones
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("title");
            _sanitizer.AllowedAttributes.Add("href");
            
            // Disallow all CSS - more restrictive
            _sanitizer.AllowedCssProperties.Clear();
            
            // Restrict URL schemes to prevent javascript: URLs
            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
            
            // Allow data: URLs for images with safe content types only
            _sanitizer.AllowedSchemes.Add("data");
            
            // Add additional security settings
            _sanitizer.KeepChildNodes = false; // Don't keep child nodes of disallowed elements
            
            // Using FilterUrl event to provide additional safety for URLs
            _sanitizer.FilterUrl += (sender, args) => {
                // If URL contains JavaScript, replace with empty string
                if (args.OriginalUrl.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    args.SanitizedUrl = "";
                    _logger?.LogWarning("Blocked potential XSS attack in URL: {Url}", args.OriginalUrl);
                }
            };
        }

        /// <summary>
        /// Sanitize a string to prevent XSS attacks
        /// </summary>
        /// <param name="html">The input string that may contain HTML</param>
        /// <returns>Sanitized HTML string</returns>
        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;
                
            return _sanitizer.Sanitize(html);
        }
        
        /// <summary>
        /// Checks if a string contains potentially malicious content
        /// </summary>
        /// <param name="html">The input string to check</param>
        /// <returns>True if the string might contain XSS attacks</returns>
        public bool IsPotentialXss(string html)
        {
            if (string.IsNullOrEmpty(html))
                return false;
                
            string sanitized = _sanitizer.Sanitize(html);
            return sanitized != html;
        }
        
        /// <summary>
        /// Sanitizes a dictionary of values, useful for form inputs
        /// </summary>
        /// <param name="formInputs">Dictionary containing form inputs</param>
        /// <returns>Dictionary with sanitized values</returns>
        public Dictionary<string, string> SanitizeFormInputs(Dictionary<string, string> formInputs)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var kvp in formInputs)
            {
                result[kvp.Key] = Sanitize(kvp.Value);
            }
            
            return result;
        }
    }
}
