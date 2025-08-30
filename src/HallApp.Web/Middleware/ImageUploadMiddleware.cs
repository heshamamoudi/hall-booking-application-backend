using System.Text.Json;
using HallApp.Application.DTOs.Vendors;

namespace HallApp.Web.Middleware;

public class ImageUploadMiddleware
{
    private readonly RequestDelegate _next;
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImageUploadMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsTargetedEndpoint(context.Request.Path))
        {
            if (context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
                context.Request.Method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
            {
                // Enable buffering so we can read the request body multiple times
                context.Request.EnableBuffering();
                
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                
                // Check if body contains base64 image data
                if (body.Contains("data:image/") || body.Contains("data:video/"))
                {
                    // Validate image uploads in the request body
                    if (ContainsInvalidMedia(body))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid format. Please upload files in the following formats: .jpg, .png, .gif, .mp4, .avi.");
                        return;
                    }

                    if (ContainsOversizedMedia(body))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("File size exceeds the 10MB limit.");
                        return;
                    }
                }

                // Reset the request body stream position to allow further processing
                context.Request.Body.Position = 0;
            }
        }

        await _next(context);
    }

    private bool IsTargetedEndpoint(PathString path) =>
        path.StartsWithSegments("/api/vendors", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/vendor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase);

    private bool ContainsInvalidMedia(string body)
    {
        // Look for base64 data URLs that don't match allowed formats
        var dataUrlPattern = @"data:([^;]+);base64,";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, dataUrlPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var mimeType = match.Groups[1].Value.ToLower();
            if (!IsValidMimeType(mimeType))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool ContainsOversizedMedia(string body)
    {
        // Extract base64 strings and estimate their size
        var base64Pattern = @"data:[^;]+;base64,([A-Za-z0-9+/=]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, base64Pattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var base64Data = match.Groups[1].Value;
            
            // Estimate file size from the base64 string
            var estimatedSize = (base64Data.Length * 3) / 4;
            
            // Account for padding
            if (base64Data.EndsWith("=="))
                estimatedSize -= 2;
            else if (base64Data.EndsWith("="))
                estimatedSize -= 1;
                
            if (estimatedSize > _maxFileSize)
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsValidMimeType(string mimeType)
    {
        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/jpg", 
            "image/png",
            "image/gif",
            "video/mp4",
            "video/avi"
        };
        
        return allowedTypes.Contains(mimeType);
    }
}
