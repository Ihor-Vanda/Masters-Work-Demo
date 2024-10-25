using System.Text;
using ModifiedCB.Settings;

namespace ModifiedCB;

public class HttpCommunication : ICommunicationStrategy
{
    private readonly HttpClient _httpClient;
    private readonly CircuitBreakerWithRetry _cb;
    private const int RequestTimeoutSeconds = 5;

    public HttpCommunication(HttpClient httpClient, CircuitBreakerWithRetry cb)
    {
        _httpClient = httpClient;
        _cb = cb;
    }

    public async Task<bool> SendMessage(CommunicationSettings settings)
    {
        if (_cb.IsOpen)
        {
            Console.WriteLine("Circuit Breaker is open! Not allows ro send HTTP request");
            return false;
        }
        else
        {
            var s_time = DateTime.Now;
            double e_time;
            for (int i = 0; i < _cb.RetryAttemt; i++)
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

                    HttpResponseMessage response = settings.HttpSettings.Method switch
                    {
                        HttpMethod m when m == HttpMethod.Get => await _httpClient.GetAsync(settings.HttpSettings.DestinationURL, cts.Token),
                        HttpMethod m when m == HttpMethod.Post => await _httpClient.PostAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
                        HttpMethod m when m == HttpMethod.Put => await _httpClient.PutAsync(settings.HttpSettings.DestinationURL, content, cts.Token),
                        HttpMethod m when m == HttpMethod.Delete => await _httpClient.DeleteAsync(settings.HttpSettings.DestinationURL, cts.Token),
                        _ => throw new NotSupportedException($"Unsupported HTTP method: {settings.HttpSettings.Method}")
                    };

                    var endTime = (DateTime.Now - startTime).TotalSeconds;
                    LibMetrics.RequestDurationTime(endTime);
                    e_time = (DateTime.Now - s_time).TotalSeconds;
                    LibMetrics.SendMessageDurationTime(e_time);
                    LibMetrics.IncSuccessfulMessages();

                    return true;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Request timed out after 5 seconds.");
                    _cb.RecordFailure();
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine($"Failed to send request! Incorrect request settings");
                    e_time = (DateTime.Now - s_time).TotalSeconds;
                    LibMetrics.SendMessageDurationTime(e_time);
                    return false;
                }
                catch
                {
                    _cb.RecordFailure();
                }

                LibMetrics.IncHttpRetryAttempts();
                if (i < _cb.RetryAttemt)
                {
                    Console.WriteLine($"HTTP request failed. Retrying in {_cb.RetryInterval.TotalSeconds} seconds...");
                    await Task.Delay(_cb.RetryInterval);
                }
            }

            e_time = (DateTime.Now - s_time).TotalSeconds;
            LibMetrics.SendMessageDurationTime(e_time);
            Console.WriteLine($"Failed to send request {_cb.RetryAttemt} times");
            LibMetrics.IncFailedMessages();
            return false;
        }
    }
}
