using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HallApp.Web.Helpers;

/// <summary>
/// Helper class to build and apply Content Security Policy headers
/// according to OWASP best practices
/// </summary>
public class ContentSecurityPolicyHelper
{
    private StringBuilder _policy;
    private bool _reportOnly;

    public ContentSecurityPolicyHelper(bool reportOnly = false)
    {
        _reportOnly = reportOnly;
        _policy = new StringBuilder();
        
        // Initialize with default restrictive policy
        SetDefaultSrc("'self'");
        SetScriptSrc("'self'");
        SetStyleSrc("'self'");
        SetImgSrc("'self'", "data:");
        SetFontSrc("'self'");
        SetConnectSrc("'self'");
        SetObjectSrc("'none'");
        SetMediaSrc("'self'");
        SetFrameSrc("'none'");
        SetBaseUri("'self'");
        SetFormAction("'self'");
    }

    /// <summary>
    /// Set the default-src directive which acts as a fallback for other directives
    /// </summary>
    public ContentSecurityPolicyHelper SetDefaultSrc(params string[] sources)
    {
        AppendDirective("default-src", sources);
        return this;
    }

    /// <summary>
    /// Set the script-src directive which controls JavaScript sources
    /// </summary>
    public ContentSecurityPolicyHelper SetScriptSrc(params string[] sources)
    {
        AppendDirective("script-src", sources);
        return this;
    }

    /// <summary>
    /// Add 'unsafe-inline' to script-src (only use in development)
    /// </summary>
    public ContentSecurityPolicyHelper AllowInlineScripts()
    {
        AppendValueToDirective("script-src", "'unsafe-inline'");
        return this;
    }

    /// <summary>
    /// Add 'unsafe-eval' to script-src (only use in development)
    /// </summary>
    public ContentSecurityPolicyHelper AllowEvalScripts()
    {
        AppendValueToDirective("script-src", "'unsafe-eval'");
        return this;
    }

    /// <summary>
    /// Set the style-src directive which controls CSS sources
    /// </summary>
    public ContentSecurityPolicyHelper SetStyleSrc(params string[] sources)
    {
        AppendDirective("style-src", sources);
        return this;
    }

    /// <summary>
    /// Set the img-src directive which controls image sources
    /// </summary>
    public ContentSecurityPolicyHelper SetImgSrc(params string[] sources)
    {
        AppendDirective("img-src", sources);
        return this;
    }

    /// <summary>
    /// Set the font-src directive which controls font sources
    /// </summary>
    public ContentSecurityPolicyHelper SetFontSrc(params string[] sources)
    {
        AppendDirective("font-src", sources);
        return this;
    }

    /// <summary>
    /// Set the connect-src directive which controls XHR, WebSockets, etc.
    /// </summary>
    public ContentSecurityPolicyHelper SetConnectSrc(params string[] sources)
    {
        AppendDirective("connect-src", sources);
        return this;
    }

    /// <summary>
    /// Set the object-src directive which controls embeddable objects like Flash
    /// </summary>
    public ContentSecurityPolicyHelper SetObjectSrc(params string[] sources)
    {
        AppendDirective("object-src", sources);
        return this;
    }

    /// <summary>
    /// Set the media-src directive which controls audio and video sources
    /// </summary>
    public ContentSecurityPolicyHelper SetMediaSrc(params string[] sources)
    {
        AppendDirective("media-src", sources);
        return this;
    }

    /// <summary>
    /// Set the frame-src directive which controls frame sources
    /// </summary>
    public ContentSecurityPolicyHelper SetFrameSrc(params string[] sources)
    {
        AppendDirective("frame-src", sources);
        return this;
    }

    /// <summary>
    /// Set the base-uri directive which restricts base tags
    /// </summary>
    public ContentSecurityPolicyHelper SetBaseUri(params string[] sources)
    {
        AppendDirective("base-uri", sources);
        return this;
    }

    /// <summary>
    /// Set the form-action directive which restricts form submission targets
    /// </summary>
    public ContentSecurityPolicyHelper SetFormAction(params string[] sources)
    {
        AppendDirective("form-action", sources);
        return this;
    }

    /// <summary>
    /// Set report-uri directive for CSP violation reporting
    /// </summary>
    public ContentSecurityPolicyHelper SetReportUri(string uri)
    {
        AppendDirective("report-uri", new[] { uri });
        return this;
    }

    /// <summary>
    /// Helper to append a directive to the policy
    /// </summary>
    private void AppendDirective(string directive, string[] sources)
    {
        if (_policy.Length > 0 && _policy[_policy.Length - 1] != ';')
        {
            _policy.Append("; ");
        }

        _policy.Append(directive).Append(" ");
        _policy.Append(string.Join(" ", sources));
    }

    /// <summary>
    /// Helper to append a value to an existing directive
    /// </summary>
    private void AppendValueToDirective(string directive, string value)
    {
        string policy = _policy.ToString();
        int directiveIndex = policy.IndexOf(directive);
        
        if (directiveIndex == -1)
        {
            // Directive not found, add it
            AppendDirective(directive, new[] { value });
            return;
        }
        
        // Find the end of the directive (next semicolon or end of string)
        int endIndex = policy.IndexOf(';', directiveIndex);
        if (endIndex == -1)
        {
            endIndex = policy.Length;
        }
        
        // Check if value already exists in the directive
        string directiveSection = policy.Substring(directiveIndex, endIndex - directiveIndex);
        if (!directiveSection.Contains(value))
        {
            // Insert the value at the end of the directive
            _policy.Insert(endIndex, " " + value);
        }
    }

    /// <summary>
    /// Apply the Content Security Policy to an HTTP response
    /// </summary>
    public void ApplyPolicy(HttpResponse response)
    {
        var headerName = _reportOnly ? 
            "Content-Security-Policy-Report-Only" : 
            "Content-Security-Policy";
            
        response.Headers[headerName] = new StringValues(_policy.ToString());
    }

    /// <summary>
    /// Get the Content Security Policy as a string
    /// </summary>
    public override string ToString()
    {
        return _policy.ToString();
    }

    /// <summary>
    /// Generate a nonce value for use with CSP
    /// </summary>
    public static string GenerateNonce()
    {
        byte[] nonceBytes = new byte[16];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonceBytes);
        }
        return Convert.ToBase64String(nonceBytes);
    }
}
