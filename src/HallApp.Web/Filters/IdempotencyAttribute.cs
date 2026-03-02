using HallApp.Application.Configuration;
using HallApp.Core.Entities;
using HallApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace HallApp.Web.Filters;

/// <summary>
/// Idempotency filter for payment operations (CRIT-SEC-002).
/// Prevents duplicate processing by caching the response (configurable expiration).
/// Usage: [Idempotency("payment", required: true)] on controller action.
/// Requires header: X-Idempotency-Key.
/// When required is true, the request is rejected with 400 if the header is missing.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IdempotencyAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "X-Idempotency-Key";

    private readonly string _operationType;
    private readonly bool _required;

    /// <summary>
    /// Creates an idempotency filter for the specified operation type.
    /// </summary>
    /// <param name="operationType">Logical name of the operation (e.g., "payment_checkout").</param>
    /// <param name="required">
    /// When true, the X-Idempotency-Key header is mandatory and the request is rejected with 400 if missing.
    /// When false (default), requests without the header proceed normally without idempotency protection.
    /// </param>
    public IdempotencyAttribute(string operationType, bool required = false)
    {
        _operationType = operationType;
        _required = required;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get idempotency key from header
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKeyValue))
        {
            if (_required)
            {
                // CRIT-SEC-002: Reject requests without idempotency key on critical endpoints
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<IdempotencyAttribute>>();
                logger.LogWarning(
                    "CRIT-SEC-002: Missing required {Header} header on {Method} {Path}. " +
                    "OperationType: {OperationType}",
                    IdempotencyKeyHeader,
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    _operationType);

                context.Result = new BadRequestObjectResult(new
                {
                    message = $"{IdempotencyKeyHeader} header is required for this operation",
                    header = IdempotencyKeyHeader
                });
                return;
            }

            // Not required - proceed without idempotency protection
            await next();
            return;
        }

        var idempotencyKey = idempotencyKeyValue.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = $"{IdempotencyKeyHeader} header cannot be empty"
            });
            return;
        }

        // Get services from DI
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var logger2 = context.HttpContext.RequestServices.GetRequiredService<ILogger<IdempotencyAttribute>>();
        var businessRules = context.HttpContext.RequestServices.GetRequiredService<IOptions<BusinessRulesSettings>>().Value;

        // Get user ID from claims
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            // Check if key exists and is not expired
            var existingKey = await unitOfWork.IdempotencyKeyRepository.GetByKeyAsync(idempotencyKey);

            if (existingKey != null)
            {
                // Key exists - return cached response
                logger2.LogInformation(
                    "Idempotency key {Key} found for user {UserId}. Returning cached response (status {StatusCode})",
                    idempotencyKey, userId, existingKey.StatusCode);

                context.HttpContext.Response.Headers.Append("X-Idempotency-Replay", "true");

                if (!string.IsNullOrEmpty(existingKey.ResponseBody))
                {
                    context.Result = new ContentResult
                    {
                        Content = existingKey.ResponseBody,
                        ContentType = "application/json",
                        StatusCode = existingKey.StatusCode
                    };
                }
                else
                {
                    context.Result = new StatusCodeResult(existingKey.StatusCode);
                }

                return;
            }

            // Key doesn't exist - execute action
            var executedContext = await next();

            // Store the result if the action succeeded
            if (executedContext.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? 200;
                var responseBody = JsonSerializer.Serialize(objectResult.Value);

                var newKey = new IdempotencyKey
                {
                    Key = idempotencyKey,
                    OperationType = _operationType,
                    UserId = userId,
                    StatusCode = statusCode,
                    ResponseBody = responseBody,
                    ProcessedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(businessRules.IdempotencyKeyExpirationHours)
                };

                await unitOfWork.IdempotencyKeyRepository.AddAsync(newKey);
                await unitOfWork.Complete();

                logger2.LogInformation(
                    "Idempotency key {Key} stored for user {UserId} (operation: {OperationType}, status: {StatusCode})",
                    idempotencyKey, userId, _operationType, statusCode);
            }
        }
        catch (Exception ex)
        {
            logger2.LogError(ex,
                "Error processing idempotency key {Key} for user {UserId}",
                idempotencyKey, userId);

            // Continue with normal execution if idempotency check fails
            await next();
        }
    }
}
