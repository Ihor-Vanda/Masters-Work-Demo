namespace ModifiedCB.Settings;

public class CircuitBreakerSettings
{
    public string? OperationMode { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public int RetryAttempts { get; set; }
    public required RabbitMqSettings RabbitMq { get; set; }
    public int FailureThreshold { get; set; }
    public int ResetTimeoutSeconds { get; set; }
}