using ModifiedCB.Settings;

namespace ModifiedCB;

public class CBCommunication : ICommunicationStrategy
{
    private readonly ICommunicationStrategy _httpStrategy;
    private readonly ICommunicationStrategy _rabbitMqStrategy;

    public CBCommunication(ICommunicationStrategy httpStrategy, ICommunicationStrategy rabbitMqStrategy)
    {
        _httpStrategy = httpStrategy;
        _rabbitMqStrategy = rabbitMqStrategy;
    }

    public async Task<bool> SendMessage(CommunicationSettings settings)
    {
        using (LibMetrics.TrackMessageSendDuration())
        {
            var httpRequest = await _httpStrategy.SendMessage(settings);
            if (httpRequest)
            {
                return true;
            }

            LibMetrics.IncRabbitMqFallbacks();
            var rabbitmqRequest = await _rabbitMqStrategy.SendMessage(settings);
            if (rabbitmqRequest)
            {
                return true;
            }

            Console.WriteLine("Can't send message both ways");
            LibMetrics.IncFailedMessages();
            return false;
        }
    }
}
