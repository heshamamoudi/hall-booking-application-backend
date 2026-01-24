namespace HallApp.Web.Middleware.RateLimiting;

/// <summary>
/// Token bucket implementation for rate limiting - shared across all rate limiting middleware
/// </summary>
public class TokenBucket
{
    private readonly int _maxTokens;
    private readonly int _refillAmount;
    private readonly TimeSpan _refillPeriod;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();

    public TokenBucket(int maxTokens, int refillAmount, TimeSpan refillPeriod)
    {
        _maxTokens = maxTokens;
        _refillAmount = refillAmount;
        _refillPeriod = refillPeriod;
        _tokens = maxTokens;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume(int count = 1)
    {
        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= count)
            {
                _tokens -= count;
                return true;
            }

            return false;
        }
    }

    public double RemainingTokens
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return _tokens;
            }
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timePassed = now - _lastRefill;

        if (timePassed >= _refillPeriod)
        {
            var periods = Math.Floor(timePassed.TotalSeconds / _refillPeriod.TotalSeconds);
            var tokensToAdd = periods * _refillAmount;
            _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
