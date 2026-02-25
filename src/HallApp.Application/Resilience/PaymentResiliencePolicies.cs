using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace HallApp.Application.Resilience;

/// <summary>
/// HIGH-004 + HIGH-010: Configures Polly retry and circuit breaker policies for payment provider HTTP clients.
/// - Retry: exponential backoff with jitter (3 retries: ~1s, ~2s, ~4s) on transient failures only.
/// - Circuit Breaker: opens after 5 consecutive failures, half-open after 60s to test recovery.
/// - Timeout: per-request timeout of 30 seconds.
/// </summary>
public static class PaymentResiliencePolicies
{
    /// <summary>
    /// Adds resilience policies (retry + circuit breaker + timeout) to a named HTTP client.
    /// Only retries on transient HTTP failures: network errors, timeouts, and 5xx server errors.
    /// </summary>
    public static IHttpClientBuilder AddPaymentResiliencePolicies(
        this IHttpClientBuilder builder, string providerName)
    {
        // 1. Retry policy with exponential backoff and jitter
        builder.AddPolicyHandler((services, _) =>
        {
            var logger = services.GetRequiredService<ILogger<HttpClient>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException, 5xx, 408
                .Or<TimeoutRejectedException>() // Polly timeout
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                    {
                        // Exponential backoff: 1s, 2s, 4s with jitter (+/- 25%)
                        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(
                            (int)(-baseDelay.TotalMilliseconds * 0.25),
                            (int)(baseDelay.TotalMilliseconds * 0.25)));
                        return baseDelay + jitter;
                    },
                    onRetry: (outcome, delay, retryAttempt, context) =>
                    {
                        var correlationId = context.TryGetValue("CorrelationId", out var corrId)
                            ? corrId?.ToString() ?? "N/A"
                            : "N/A";

                        logger.LogWarning(
                            "HIGH-004: Retry {RetryAttempt}/3 for {Provider} after {Delay}ms. " +
                            "CorrelationId: {CorrelationId}, " +
                            "StatusCode: {StatusCode}, Error: {Error}",
                            retryAttempt,
                            providerName,
                            delay.TotalMilliseconds,
                            correlationId,
                            outcome.Result?.StatusCode.ToString() ?? "N/A",
                            outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                    });
        });

        // 2. Circuit breaker policy
        builder.AddPolicyHandler((services, _) =>
        {
            var logger = services.GetRequiredService<ILogger<HttpClient>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(60),
                    onBreak: (outcome, breakDuration) =>
                    {
                        logger.LogError(
                            "HIGH-010: Circuit OPENED for {Provider}. " +
                            "Will remain open for {BreakDuration}s. " +
                            "Last error: {Error}",
                            providerName,
                            breakDuration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                    },
                    onReset: () =>
                    {
                        logger.LogInformation(
                            "HIGH-010: Circuit CLOSED for {Provider}. Service recovered.",
                            providerName);
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation(
                            "HIGH-010: Circuit HALF-OPEN for {Provider}. Testing recovery...",
                            providerName);
                    });
        });

        // 3. Per-request timeout of 30 seconds (inner timeout, inside the retry)
        builder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(30));

        return builder;
    }
}
