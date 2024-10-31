using System.Text;
using ModifiedCB.Settings;
using ModifiedCB.CircuitBreaker;

namespace ModifiedCB;

public class HttpCommunication : ICommunicationStrategy
{
    private readonly HttpClient _httpClient;
    private readonly CircuitBreakerWithCallBack _cb;
    private const int RequestTimeoutSeconds = 3;
    private int _retryAttemt;
    private TimeSpan _retryTimeout;

    public HttpCommunication(
        HttpClient httpClient,
        CircuitBreakerWithCallBack cb,
        int retryAttempt,
        TimeSpan retryTimeout)
    {
        _httpClient = httpClient;
        _cb = cb;
        _retryAttemt = retryAttempt > 0 ? retryAttempt : 1;
        _retryTimeout = retryTimeout > TimeSpan.FromMicroseconds(500) ? retryTimeout : TimeSpan.FromMicroseconds(500);
    }

    public async Task<bool> SendMessage(CommunicationSettings settings)
    {
        if (_cb.GetState() == CircuitBreakerState.Open)
        {
            Console.WriteLine("Circuit Breaker is open! Not allows to send HTTP request");
            return false;
        }

        var s_time = DateTime.Now;
        double e_time;

        for (int i = 0; i < _retryAttemt; i++)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(settings);
                ArgumentNullException.ThrowIfNull(settings.HttpSettings);
                ArgumentNullException.ThrowIfNull(settings.HttpSettings.DestinationURL);
                ArgumentNullException.ThrowIfNull(settings.HttpSettings.Method);

                StringContent? content = null;
                if (settings.HttpSettings.Message != null)
                {
                    content = new StringContent(settings.HttpSettings.Message, Encoding.UTF8, "application/json");
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
                var startTime = DateTime.Now;
                double endTime;


                bool success = await _cb.ExecuteAction(async () =>
                {
                    try
                    {
                        HttpResponseMessage response = settings.HttpSettings.Method switch
                        {
                            HttpMethod m when m == HttpMethod.Get => await _httpClient.GetAsync(settings.HttpSettings.DestinationURL, cts.Token),
                            HttpMethod m when m == HttpMethod.Post => await _httpClient.PostAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
                            HttpMethod m when m == HttpMethod.Put => await _httpClient.PutAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
                            HttpMethod m when m == HttpMethod.Delete => await _httpClient.DeleteAsync(settings.HttpSettings.DestinationURL, cts.Token),
                            _ => throw new NotSupportedException($"Unsupported HTTP method: {settings.HttpSettings.Method}")
                        };

                        response.EnsureSuccessStatusCode();
                        return true;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"HTTP request error: {ex.Message}");
                        return false;
                    }
                });

                if (success)
                {
                    endTime = (DateTime.Now - startTime).TotalSeconds;
                    LibMetrics.RequestDurationTime(endTime);
                    e_time = (DateTime.Now - s_time).TotalSeconds;
                    LibMetrics.SendMessageDurationTime(e_time);
                    LibMetrics.IncSuccessfulMessages();
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Request timed out after {RequestTimeoutSeconds} seconds.");
                e_time = (DateTime.Now - s_time).TotalSeconds;
                LibMetrics.SendMessageDurationTime(e_time);
                return false;
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Failed to send request! Incorrect request settings: {ex.Message}");
                e_time = (DateTime.Now - s_time).TotalSeconds;
                LibMetrics.SendMessageDurationTime(e_time);
                return false;
            }

            LibMetrics.IncHttpRetryAttempts();
            if (i < _retryAttemt - 1)
            {
                Console.WriteLine($"HTTP request failed. Retrying in {_retryTimeout.TotalMilliseconds} milliseconds...");
                await Task.Delay(_retryTimeout);
            }
        }

        e_time = (DateTime.Now - s_time).TotalSeconds;
        LibMetrics.SendMessageDurationTime(e_time);
        Console.WriteLine($"Failed to send request after {_retryAttemt} attempts.");
        LibMetrics.IncFailedMessages();
        return false;
    }
}

// HttpResponseMessage response = settings.HttpSettings.Method switch
// {
//     HttpMethod m when m == HttpMethod.Get => await _httpClient.GetAsync(settings.HttpSettings.DestinationURL, cts.Token),
//     HttpMethod m when m == HttpMethod.Post => await _httpClient.PostAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
//     HttpMethod m when m == HttpMethod.Put => await _httpClient.PutAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
//     HttpMethod m when m == HttpMethod.Delete => await _httpClient.DeleteAsync(settings.HttpSettings.DestinationURL, cts.Token),
//     _ => throw new NotSupportedException($"Unsupported HTTP method: {settings.HttpSettings.Method}")
// };

// var endTime = (DateTime.Now - startTime).TotalSeconds;
// LibMetrics.RequestDurationTime(endTime);
// e_time = (DateTime.Now - s_time).TotalSeconds;
// LibMetrics.SendMessageDurationTime(e_time);
// LibMetrics.IncSuccessfulMessages();

// return true;
//     }
//             catch (OperationCanceledException)
//             {
//             Console.WriteLine("Request timed out after 5 seconds.");
//             // _cb.RecordFailure();
//         }
//             catch (ArgumentNullException)
//             {
//             Console.WriteLine($"Failed to send request! Incorrect request settings");
//             e_time = (DateTime.Now - s_time).TotalSeconds;
//             LibMetrics.SendMessageDurationTime(e_time);
//             return false;
//         }
//             catch
//             {
//     // _cb.RecordFailure();
// }

// LibMetrics.IncHttpRetryAttempts();
// if (i < _retryAttemt)
// {
//     Console.WriteLine($"HTTP request failed. Retrying in {_retryTimeout.TotalMilliseconds} miliseconds...");
//     await Task.Delay(_retryTimeout);
// }
//     }

//     e_time = (DateTime.Now - s_time).TotalSeconds;
// LibMetrics.SendMessageDurationTime(e_time);
// Console.WriteLine($"Failed to send request {_retryAttemt} times");
// LibMetrics.IncFailedMessages();
// return false;

//     }
// }
