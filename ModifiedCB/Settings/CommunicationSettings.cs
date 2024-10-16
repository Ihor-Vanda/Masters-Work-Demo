namespace ModifiedCB.Settings;

public class CommunicationSettings
{
    public HttpCommunicationSettings? HttpSettings { get; set; }
    public RabbitMqCommunicationSettings? RabbitMqSettings { get; set; }
}