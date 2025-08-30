using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Text;

namespace HallApp.Application.Services
{
    /// <summary>
    /// Service to protect against Cross-Site Scripting (XSS) attacks according to OWASP guidelines
    /// </summary>
    public class XssProtectionService
    {
        private readonly ILogger<XssProtectionService> _logger;
        private readonly Regex _xssPattern;

        public XssProtectionService(ILogger<XssProtectionService> logger)
        {
            _logger = logger;
            
            // Comprehensive XSS detection pattern
            _xssPattern = new Regex(
                @"<script[^>]*>.*?</script>|javascript:|onerror=|onload=|eval\(|setTimeout\(|setInterval\(|new\s+Function\(|document\.cookie|document\.write|window\.location|<iframe|<object|<embed|<img[^>]*\s+src\s*=|<svg[^>]*\s+onload\s*=",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
            );
        }

        /// <summary>
        /// Checks if the input contains potential XSS attack vectors
        /// </summary>
        /// <param name="input">The string to validate</param>
        /// <returns>True if the input is safe, false if potential XSS is detected</returns>
        public bool IsSafeFromXss(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true; // Empty input is safe
            }

            bool isSafe = !_xssPattern.IsMatch(input);
            
            if (!isSafe)
            {
                _logger.LogWarning("Potential XSS attack detected in input");
            }
            
            return isSafe;
        }

        /// <summary>
        /// Sanitizes user input to prevent XSS attacks
        /// </summary>
        /// <param name="input">The string to sanitize</param>
        /// <returns>Sanitized string safe for rendering in HTML</returns>
        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            
            // HTML encode the input to prevent script execution
            string sanitized = HttpUtility.HtmlEncode(input);
            
            return sanitized;
        }

        /// <summary>
        /// Sanitizes HTML content using a whitelist approach (for scenarios where some HTML is allowed)
        /// </summary>
        /// <param name="html">The HTML content to sanitize</param>
        /// <returns>Sanitized HTML with only allowed tags and attributes</returns>
        public string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            
            // Define allowed tags and attributes
            HashSet<string> allowedTags = new(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "strong", "em", "u", "h1", "h2", "h3", "h4", "h5", "h6",
                "ul", "ol", "li", "a", "span", "div", "blockquote", "pre", "code"
            };
            
            HashSet<string> allowedAttributes = new(StringComparer.OrdinalIgnoreCase)
            {
                "href", "title", "style", "class", "target", "id"
            };
            
            // Basic implementation of HTML sanitization
            // Note: For production use, a dedicated library like HtmlSanitizer is recommended
            
            // First replace all < and > characters that are not part of tags
            html = Regex.Replace(html, @"<(?!(/?(" + string.Join("|", allowedTags) + ")\b))", "&lt;");
            
            // Then find all tags and check if they're allowed
            StringBuilder sanitized = new();
            int lastIndex = 0;
            
            foreach (Match match in Regex.Matches(html, @"<(/?)(\w+)([^>]*)>"))
            {
                string fullTag = match.Value;
                string tagName = match.Groups[2].Value.ToLowerInvariant();
                string attributes = match.Groups[3].Value;
                
                // Add text before the current tag
                sanitized.Append(html.Substring(lastIndex, match.Index - lastIndex));
                lastIndex = match.Index + match.Length;
                
                // Check if the tag is allowed
                if (!allowedTags.Contains(tagName))
                {
                    sanitized.Append(HttpUtility.HtmlEncode(fullTag));
                    continue;
                }
                
                // Start building the sanitized tag
                sanitized.Append("<").Append(match.Groups[1].Value).Append(tagName);
                
                // Process attributes if any
                if (!string.IsNullOrWhiteSpace(attributes))
                {
                    foreach (Match attrMatch in Regex.Matches(attributes, @"(\w+)\s*=\s*[""']([^""']*)[""']"))
                    {
                        string attrName = attrMatch.Groups[1].Value.ToLowerInvariant();
                        string attrValue = attrMatch.Groups[2].Value;
                        
                        // Check if the attribute is allowed
                        if (allowedAttributes.Contains(attrName))
                        {
                            // Special handling for href attributes to prevent javascript: URLs
                            if (attrName == "href" && (attrValue.Contains("javascript:") || attrValue.Contains("data:")))
                            {
                                continue;
                            }
                            
                            // Special handling for style attributes to prevent expression()
                            if (attrName == "style" && attrValue.Contains("expression") || attrValue.Contains("url("))
                            {
                                continue;
                            }
                            
                            sanitized.Append(" ").Append(attrName).Append("=\"")
                                .Append(HttpUtility.HtmlAttributeEncode(attrValue)).Append("\"");
                        }
                    }
                }
                
                sanitized.Append(">");
            }
            
            // Add any remaining text
            if (lastIndex < html.Length)
            {
                sanitized.Append(html.Substring(lastIndex));
            }
            
            return sanitized.ToString();
        }
    }
}
