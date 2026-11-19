namespace VisitorCounterApi.Services;

// Simple protection against floods: at most N requests per time window.
// It lives in memory of one running instance, which is enough for a small portfolio site.
public sealed class FixedWindowRateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly object _lock = new();
    private DateTime _windowStart = DateTime.UtcNow;
    private int _count;

    public FixedWindowRateLimiter(int maxRequests, TimeSpan window)
    {
        _maxRequests = maxRequests;
        _window = window;
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _windowStart >= _window)
            {
                _windowStart = now;
                _count = 0;
            }

            if (_count >= _maxRequests)
            {
                return false;
            }

            _count++;
            return true;
        }
    }
}