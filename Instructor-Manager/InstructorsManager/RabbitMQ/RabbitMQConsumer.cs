using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using InstructorsManager.Repository;

namespace InstructorsManager.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly SemaphoreSlim _semaphore = new(1, 1); // Семафор для контролю обробки


        public RabbitMQConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "user",
                Password = "password"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.QueueDeclare(queue: "instructor-course",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    if (!IsValidJson(messageJson))
                    {
                        Console.WriteLine("Invalid JSON message received.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    var message = JsonSerializer.Deserialize<RabbitMQMessage>(messageJson);

                    if (message == null || string.IsNullOrEmpty(message.Type) || string.IsNullOrEmpty(message.CourseId) || message.EntityIds == null || message.EntityIds.Count == 0)
                    {
                        Console.WriteLine("Invalid message received.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    try
                    {
                        switch (message.Type)
                        {
                            case "add":
                                await HandleAddCourseRequest(message);
                                break;
                            case "delete":
                                await HandleDeleteCourseRequestAsync(message);
                                break;
                            default:
                                Console.WriteLine($"Unknown message type: {message.Type}");
                                break;
                        }
                    }
                    finally
                    {
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            };

            _channel.BasicConsume(queue: "instructor-course",
                                 autoAck: false,
                                 consumer: consumer);

            await Task.CompletedTask;
        }

        private bool IsValidJson(string jsonString)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private async Task HandleDeleteCourseRequestAsync(RabbitMQMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

            var instructors = await repo.GetInstructorsAsync();

            if (instructors == null || instructors.Count == 0)
            {
                Console.WriteLine("Can't get instructors from repo");
                return;
            }

            var list = instructors.FindAll(s => message.EntityIds != null && message.EntityIds.Any(id => id != null && id == s.Id));

            if (list == null || list.Count == 0)
            {
                Console.WriteLine($"Instructors not found for deleting course {message.CourseId}.");
                return;
            }

            foreach (var instructor in list)
            {
                ArgumentNullException.ThrowIfNull(message.CourseId);
                if (instructor.Courses.Contains(message.CourseId))
                {
                    ArgumentNullException.ThrowIfNull(instructor.Id);
                    var res = await repo.DeleteCourseAsync(instructor.Id, message.CourseId);
                    Console.WriteLine($"Updated instructor {instructor.Id} - Success: {res.ModifiedCount > 0}");
                    Console.WriteLine($"Deleted course {message.CourseId} from instructor {instructor.Id}.");
                }
                else
                {
                    Console.WriteLine($"The instructor {instructor.Id} does not have course {message.CourseId}");
                }
            }
        }

        private async Task HandleAddCourseRequest(RabbitMQMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

            var instructors = await repo.GetInstructorsAsync();

            if (instructors == null || instructors.Count == 0)
            {
                Console.WriteLine("Can't get instructors from repo");
                return;
            }

            var list = instructors.FindAll(s => message.EntityIds != null && message.EntityIds.Any(id => id != null && id == s.Id));

            if (list == null || list.Count == 0)
            {
                Console.WriteLine($"Instructors not found for adding course {message.CourseId}.");
                return;
            }

            foreach (var instructor in list)
            {
                ArgumentNullException.ThrowIfNull(message.CourseId);
                if (!instructor.Courses.Contains(message.CourseId))
                {
                    ArgumentNullException.ThrowIfNull(instructor.Id);
                    var res = await repo.AddCourseAsync(instructor.Id, message.CourseId);
                    Console.WriteLine($"Updated instructor {instructor.Id} - Success: {res.ModifiedCount > 0}");
                    Console.WriteLine($"Add course {message.CourseId} to instructor {instructor.Id}.");
                }
                else
                {
                    Console.WriteLine($"The instructor {instructor.Id} already has course {message.CourseId}.");
                }
            }
        }
    }
}
