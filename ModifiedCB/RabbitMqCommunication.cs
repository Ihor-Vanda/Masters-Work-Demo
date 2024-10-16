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

    public async Task SendMessage(CommunicationSettings settings)
    {
        var body = Encoding.UTF8.GetBytes(settings.RabbitMqSettings.Message);

        _channel.BasicPublish(exchange: "", routingKey: settings.RabbitMqSettings.QueueName, basicProperties: null, body: body);
        await Task.CompletedTask;
    }
}