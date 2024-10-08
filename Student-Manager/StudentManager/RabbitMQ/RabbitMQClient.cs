using System.Text;
using RabbitMQ.Client;

namespace StudentManager.RabbitMQ;

public class RabbitMQClient
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQClient()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            Port = 5672
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void PublishMessage(string queueName, string message)
    {
        _channel.QueueDeclare(queue: queueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(exchange: "",
                            routingKey: queueName,
                            basicProperties: null,
                            body: body);

    }

    public void Disponse()
    {
        _channel.Close();
        _connection.Close();
    }
}