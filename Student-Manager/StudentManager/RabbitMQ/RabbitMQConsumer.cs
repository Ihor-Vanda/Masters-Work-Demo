using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StudentManager.Repository;

namespace StudentManager.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Семафор для контролю обробки

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
            _channel.QueueDeclare(queue: "student-course",
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

                    // Перевірка, чи повідомлення є валідним JSON
                    if (!IsValidJson(messageJson))
                    {
                        Console.WriteLine("Invalid JSON message received.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    var message = JsonSerializer.Deserialize<RabbitMQMessage>(messageJson);

                    if (message == null || string.IsNullOrEmpty(message.Type) || string.IsNullOrEmpty(message.CourseId) || message.EntityIds == null || message.EntityIds.Count == 0)
                    {
                        Console.WriteLine("Invalid message structure.");
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
                                await HandleDeleteCourseRequest(message);
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

            _channel.BasicConsume(queue: "student-course",
                                 autoAck: false,
                                 consumer: consumer);

            await Task.CompletedTask;
        }

        // Функція для перевірки валідності JSON
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

        private async Task HandleDeleteCourseRequest(RabbitMQMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var studentRepository = scope.ServiceProvider.GetRequiredService<IRepository>();

            var students = await studentRepository.GetAllStudents();

            if (students == null || students.Count == 0)
            {
                Console.WriteLine("Can't get students from repo");
                return;
            }

            var list = students.FindAll(s => message.EntityIds != null && message.EntityIds.Any(id => id != null && id == s.Id));

            if (list == null || list.Count == 0)
            {
                Console.WriteLine($"Students not found for deleting course {message.CourseId}.");
                return;
            }

            foreach (var student in list)
            {
                ArgumentNullException.ThrowIfNull(message.CourseId);
                if (student.Courses.Contains(message.CourseId))
                {
                    ArgumentNullException.ThrowIfNull(student.Id);
                    var res = await studentRepository.DeleteCourseAsync(student.Id, message.CourseId);
                    Console.WriteLine($"Updated student {student.Id} - Success: {res.ModifiedCount > 0}");
                    Console.WriteLine($"Deleted course {message.CourseId} from student {student.Id}.");
                }
                else
                {
                    Console.WriteLine($"The student {student.Id} does not have course {message.CourseId}.");
                }
            }
        }

        private async Task HandleAddCourseRequest(RabbitMQMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var studentRepository = scope.ServiceProvider.GetRequiredService<IRepository>();
            var students = await studentRepository.GetAllStudents();

            if (students == null || students.Count == 0)
            {
                Console.WriteLine("Can't get students from repo");
                return;
            }

            var list = students.FindAll(s => message.EntityIds != null && message.EntityIds.Any(id => id != null && id == s.Id));

            if (list == null || list.Count == 0)
            {
                Console.WriteLine($"Students not found for adding course {message.CourseId}.");
                return;
            }

            foreach (var student in list)
            {
                ArgumentNullException.ThrowIfNull(message.CourseId);
                if (!student.Courses.Contains(message.CourseId))
                {
                    ArgumentNullException.ThrowIfNull(student.Id);
                    var res = await studentRepository.AddCourseAsync(student.Id, message.CourseId);
                    Console.WriteLine($"Updated student {student.Id} - Success: {res.ModifiedCount > 0}");
                    Console.WriteLine($"Added course {message.CourseId} to student {student.Id}.");
                }
                else
                {
                    Console.WriteLine($"The student {student.Id} already has course {message.CourseId}.");
                }
            }
        }
    }
}
