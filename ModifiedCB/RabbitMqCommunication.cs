using System.Text;
using ModifiedCB.Settings;
using RabbitMQ.Client;

namespace ModifiedCB;

public class RabbitMqCommunication : ICommunicationStrategy
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqCommunication(IConnection connection)
    {
        _connection = connection;
        _channel = connection.CreateModel();
    }

    public async Task<bool> SendMessage(CommunicationSettings settings)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settings.RabbitMqSettings);
            ArgumentNullException.ThrowIfNull(settings.RabbitMqSettings.QueueName);
            ArgumentNullException.ThrowIfNull(settings.RabbitMqSettings.Message);

            var body = Encoding.UTF8.GetBytes(settings.RabbitMqSettings.Message);

            var startTime = DateTime.Now;
            _channel.BasicPublish(exchange: "", routingKey: settings.RabbitMqSettings.QueueName, basicProperties: null, body: body);
            var queueLatency = (DateTime.Now - startTime).TotalSeconds;

            LibMetrics.ObserveQueueLatency(queueLatency);

            LibMetrics.IncSuccessfulMessages();
            await Task.CompletedTask;
            return true;
        }
        catch
        {
            LibMetrics.IncFailedMessages();
            return false;
        }
    }
}
