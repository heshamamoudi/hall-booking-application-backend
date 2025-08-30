using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HallApp.Core.Exceptions;

namespace HallApp.Web.Controllers
{
    [AllowAnonymous]
    public class FallBackController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FallBackController> _logger;

        public FallBackController(IWebHostEnvironment env, ILogger<FallBackController> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Root endpoint that redirects to Swagger UI
        /// </summary>
        [HttpGet("/")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()
        {
            return Redirect("/swagger");
        }

        /// <summary>
        /// Health check endpoint to verify service is running
        /// </summary>
        [HttpGet("/health")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = _env.EnvironmentName
            });
        }

#if DEBUG
        /// <summary>
        /// Test API endpoint for development
        /// </summary>
        [HttpGet("/test-api")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult TestApi()
        {
            var info = new
            {
                Title = "API Status",
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Version = "1.0",
                Environment = _env.EnvironmentName,
                Message = "API is up and running!"
            };

            return Ok(info);
        }

        /// <summary>
        /// Diagnostic information for development
        /// </summary>
        [HttpGet("/diagnostics")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Diagnostics()
        {
            var diagnosticInfo = new Dictionary<string, object>
            {
                { "Timestamp", DateTime.UtcNow },
                { "Machine", Environment.MachineName },
                { "OS", Environment.OSVersion.ToString() },
                { "ProcessUptime", (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMinutes },
                { "WebRootPath", _env?.WebRootPath ?? "Not Available" },
                { "ContentRootPath", _env?.ContentRootPath ?? "Not Available" },
                { "EnvironmentName", _env?.EnvironmentName ?? "Not Available" },
            };

            return Ok(diagnosticInfo);
        }

        /// <summary>
        /// Test error endpoint for development
        /// </summary>
        [HttpGet("/error/test/{code}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult TestError(string code)
        {
            return code switch
            {
                "401" => Unauthorized(new ApiResponse(401)),
                "403" => Forbid(),
                "404" => NotFound(new ApiResponse(404)),
                "400" => BadRequest(new ApiResponse(400)),
                "500" => StatusCode(500, new ApiResponse(500)),
                _ => BadRequest(new ApiResponse(400, "Invalid error code"))
            };
        }
#endif

        /// <summary>
        /// CatchAll handler for fallback routes - must be registered via endpoint routing (MapFallbackToController)
        /// This method should NOT handle Swagger or API routes - the middleware ordering in Program.cs ensures
        /// that Swagger middleware handles its own routes before this fallback is reached
        /// </summary>
        public IActionResult CatchAll(string url)
        {
            // Debug logging - helpful for troubleshooting which routes get here
            _logger.LogDebug("CatchAll invoked for: {Url}", url ?? "(null)");
            
            // If this is a Swagger request that somehow got here, don't handle it
            // This should not happen with proper middleware ordering, but it's a safeguard
            if (url != null && url.StartsWith("swagger", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Fallback controller received Swagger request: {Path}", url);
                return new EmptyResult(); // Let other middleware handle it
            }
            
            // API routes that aren't found should return a proper API error response
            if (url != null && url.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unknown API route: {Path}", url);
                return NotFound(new ApiResponse(404, $"API endpoint '{url}' not found"));
            }

            try
            {
                // For all non-API routes that reach here, serve the SPA index.html
                // This enables client-side routing in the SPA
                if (_env?.WebRootPath == null)
                {
                    _logger.LogError("WebRootPath is null, cannot serve SPA index.html");
                    return NotFound(new ApiResponse(404, "Server configuration error"));
                }

                var indexPath = Path.Combine(_env.WebRootPath, "index.html");
                if (System.IO.File.Exists(indexPath))
                {
                    return PhysicalFile(indexPath, "text/html");
                }
                else
                {
                    _logger.LogError("SPA index.html not found at expected path: {Path}", indexPath);
                    return NotFound(new ApiResponse(404, "SPA index file not found"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving SPA index.html for path: {Path}", url);
                return StatusCode(500, new ApiResponse(500, "Internal server error"));
            }
        }
    }
}
