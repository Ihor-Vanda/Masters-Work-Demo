using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace ModifiedCB.CircuitBreaker;

public class CircuitBreakerWithCallBack
{
    private readonly int failureThreshold;
    private readonly TimeSpan recoveryTime;
    private CircuitBreakerState state = CircuitBreakerState.Closed;

    private DateTime lastFailureTime;
    private Dictionary<string, int> errorCounts = new();

    private Dictionary<HttpStatusCode, Action> errorCallbacks = new();

    public CircuitBreakerWithCallBack(int threshold, TimeSpan recoveryDelay)
    {
        LibMetrics.SetCircuitBreakerState(1);
        failureThreshold = threshold > 0 ? threshold : 1;
        recoveryTime = recoveryDelay > TimeSpan.FromSeconds(10) ? recoveryDelay : TimeSpan.FromSeconds(10);
    }

    public void AddErrorCallback(HttpStatusCode statusCode, Action callback)
    {
        errorCallbacks[statusCode] = callback;
    }

    private void CheckState()
    {
        if (state == CircuitBreakerState.Open && DateTime.Now >= lastFailureTime.AddSeconds(recoveryTime.TotalSeconds * 2 / 3))
        {
            state = CircuitBreakerState.HalfOpen;
            Console.WriteLine("Circuit Breaker is now Half-Open, testing connection...");
        }
    }


    public async Task<bool> ExecuteAction(Func<Task<bool>> action)
    {
        CheckState();

        if (state == CircuitBreakerState.Open)
        {
            return false;
        }

        try
        {
            bool isSuccess = await action();
            if (isSuccess)
            {
                if (GetState() == CircuitBreakerState.HalfOpen)
                    Reset();
            }
            else
            {
                RegisterFailure(HttpStatusCode.InternalServerError);
            }
            return isSuccess;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode.HasValue)
            {
                RegisterFailure(ex.StatusCode.Value);
            }
            return false;
        }
    }

    private void Reset()
    {
        errorCounts.Clear();
        state = CircuitBreakerState.Closed;
        LibMetrics.SetCircuitBreakerState(1);
        Console.WriteLine("Circuit Breaker is now Closed after successful test before recovery time.");
    }

    private void RegisterFailure(HttpStatusCode statusCode)
    {
        if (errorCallbacks.ContainsKey(statusCode))
        {
            errorCallbacks[statusCode].Invoke();
        }

        string errorKey = statusCode.ToString();
        if (!errorCounts.ContainsKey(errorKey))
        {
            errorCounts[errorKey] = 0;
        }

        errorCounts[errorKey]++;

        int totalFailures = errorCounts.Values.Sum();
        if (totalFailures >= failureThreshold || errorCounts.ContainsValue(failureThreshold))
        {
            state = CircuitBreakerState.Open;
            LibMetrics.SetCircuitBreakerState(0);
            lastFailureTime = DateTime.Now;
            Console.WriteLine("Circuit Breaker moved to Open state due to failures.");

            Task.Delay(recoveryTime).ContinueWith(_ => Reset());
        }
    }

    public CircuitBreakerState GetState()
    {
        return state;
    }
}