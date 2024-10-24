using Prometheus;

namespace ModifiedCB;

public static class LibMetrics
{
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("cb_request_duration_seconds", "Duration of requests sent by the ModifiedCB library in seconds.");

    private static readonly Histogram ProcessSendingMessage = Metrics
        .CreateHistogram("cb_sending_duration_seconds", "Duration of requests from process to sent by the ModifiedCB library in seconds.");

    private static readonly Counter SuccessfulMessages = Metrics
        .CreateCounter("cb_successful_messages_total", "Total number of successfully sent messages.");

    private static readonly Counter FailedMessages = Metrics
        .CreateCounter("cb_failed_messages_total", "Total number of failed messages.");

    private static readonly Counter RabbitMqFallbacks = Metrics
        .CreateCounter("cb_rabbitmq_fallback_total", "Total number of times the system switched to RabbitMQ.");

    private static readonly Counter HttpRetryAttempts = Metrics
        .CreateCounter("cb_http_retry_attempts_total", "Total number of HTTP retry attempts.");

    private static readonly Gauge CircuitBreakerState = Metrics
        .CreateGauge("cb_circuit_breaker_state", "Current state of the Circuit Breaker (1 = open, 0 = closed).");

    private static readonly Counter Timeouts = Metrics
        .CreateCounter("cb_timeouts_total", "Total number of timeouts.");

    private static readonly Histogram QueueLatency = Metrics
        .CreateHistogram("cb_rabbitmq_queue_latency_seconds", "Time spent in RabbitMQ queue before being processed.");

    public static void IncSuccessfulMessages() => SuccessfulMessages.Inc();
    public static void IncFailedMessages() => FailedMessages.Inc();
    public static void IncRabbitMqFallbacks() => RabbitMqFallbacks.Inc();
    public static void IncHttpRetryAttempts() => HttpRetryAttempts.Inc();
    public static void SetCircuitBreakerState(int state) => CircuitBreakerState.Set(state);
    public static void IncTimeouts() => Timeouts.Inc();
    public static void ObserveQueueLatency(double latency) => QueueLatency.Observe(latency);
    public static void RequestDurationTime(double time) => RequestDuration.Observe(time);
    public static void SendMessageDurationTime(double time) => ProcessSendingMessage.Observe(time);
}
