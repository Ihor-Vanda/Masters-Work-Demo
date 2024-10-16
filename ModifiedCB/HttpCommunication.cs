using System.Text;
using ModifiedCB.Settings;

namespace ModifiedCB;

public class HttpCommunication : ICommunicationStrategy
{
    private readonly HttpClient _httpClient;

    public HttpCommunication(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendMessage(CommunicationSettings settings)
    {
        var content = new StringContent(settings.HttpSettings.Message, Encoding.UTF8, "application/json");

        HttpResponseMessage response = settings.HttpSettings.Method switch
        {
            HttpMethod m when m == HttpMethod.Get => await _httpClient.GetAsync(settings.HttpSettings.DestinationURL),
            HttpMethod m when m == HttpMethod.Post => await _httpClient.PostAsync(settings.HttpSettings.DestinationURL, content),
            HttpMethod m when m == HttpMethod.Put => await _httpClient.PutAsync(settings.HttpSettings.DestinationURL, content),
            HttpMethod m when m == HttpMethod.Delete => await _httpClient.DeleteAsync(settings.HttpSettings.DestinationURL),
            _ => throw new NotSupportedException($"Unsupported HTTP method: {settings.HttpSettings.Method}")
        };
    }
}
