namespace ModifiedCB;

public class CircuitBreakerWithRetry
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    public int RetryAttemt { get; private set; }
    public TimeSpan RetryInterval { get; private set; }
    private int FailureCount { get; set; } = 0;
    public bool IsOpen { get; private set; } = false;

    public CircuitBreakerWithRetry(
        int failureThreshold,
        TimeSpan resetTimeout,
        int retryAttempt,
        TimeSpan retryInterval)
    {
        _failureThreshold = failureThreshold > 0 ? failureThreshold : 2;
        _resetTimeout = resetTimeout > TimeSpan.FromSeconds(0) ? resetTimeout : TimeSpan.FromSeconds(30);
        RetryAttemt = retryAttempt > 0 ? retryAttempt : 1;
        RetryInterval = retryInterval > TimeSpan.FromMilliseconds(0) ? RetryInterval : TimeSpan.FromMilliseconds(500);
        LibMetrics.SetCircuitBreakerState(1);
    }

    public void RecordFailure()
    {
        FailureCount++;
        if (FailureCount >= _failureThreshold)
        {
            IsOpen = true;

            LibMetrics.SetCircuitBreakerState(0);
            Console.WriteLine($"Circuit Breaker is open for {_resetTimeout} seconds");

            Task.Delay(_resetTimeout).ContinueWith(t =>
            {
                Reset();
            });
        }
    }

    public void Reset()
    {
        FailureCount = 0;
        IsOpen = false;
        LibMetrics.SetCircuitBreakerState(1);
        Console.WriteLine("Circuit Breaker is reset");
    }
}
