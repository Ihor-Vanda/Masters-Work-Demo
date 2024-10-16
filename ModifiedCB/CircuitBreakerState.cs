public class CircuitBreakerState
{
    public bool IsOpen { get; set; }
    public int FailureCount { get; set; }
    public int FailureThreshold { get; set; }  // Поріг невдач
    public TimeSpan ResetTimeout { get; set; }  // Таймаут перезавантаження після помилок

    // Модифікований конструктор, який приймає параметри ззовні
    public CircuitBreakerState(int failureThreshold, TimeSpan resetTimeout)
    {
        IsOpen = false;
        FailureCount = 0;
        FailureThreshold = failureThreshold;  // Поріг невдач
        ResetTimeout = resetTimeout;  // Таймаут перезапуску
    }

    public void RecordFailure()
    {
        FailureCount++;
        if (FailureCount >= FailureThreshold)
        {
            IsOpen = true;
            Task.Delay(ResetTimeout).ContinueWith(t =>
            {
                IsOpen = false;
                FailureCount = 0;
            });
        }
    }

    public void Reset()
    {
        FailureCount = 0;
        IsOpen = false;
    }
}
