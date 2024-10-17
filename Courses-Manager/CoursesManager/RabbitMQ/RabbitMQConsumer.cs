using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CoursesManager.Repository;
using MongoDB.Bson;

namespace CoursesManager.RabbitMQ;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare(queue: "instructor-delete",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        _channel.QueueDeclare(queue: "student-delete",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var routingKey = ea.RoutingKey;

            var message = Encoding.UTF8.GetString(body);
            switch (routingKey)
            {
                case "instructor-delete":
                    await HandleDeleteInstructor(message);
                    break;

                case "student-delete":
                    await HandleDeleteStudent(message);
                    break;
            }
        };

        _channel.BasicConsume(queue: "instructor-delete",
                             autoAck: true,
                             consumer: consumer);

        _channel.BasicConsume(queue: "student-delete",
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task HandleDeleteStudent(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("Received an empty delete student request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        if (!ObjectId.TryParse(id, out var _id))
        {
            Console.WriteLine("Invalid id");
            return;
        }

        var courses = await repo.GetAllCoursesAsync();

        if (courses == null || courses.Count == 0)
        {
            Console.WriteLine("Can't get courses from repo");
            return;
        }
        var list = courses.FindAll(s => s.Students.Contains(id));
        if (list == null || list.Count == 0)
        {
            Console.WriteLine($"Course not found for deleting student {id}.");
            return;
        }
        for (int i = 0; i < list.Count; i++)
        {
            var course = list[i];

            if (course.Students.Contains(id))
            {
                ArgumentNullException.ThrowIfNull(course.Id);
                var res = await repo.RemoveStudentAsync(course.Id, id);

                Console.WriteLine($"Updated course {course.Id} - Success: {res.ModifiedCount > 0}");
                Console.WriteLine($"Deleted student {id} from course {course.Id}.");
            }
            else
            {
                Console.WriteLine($"The course {course.Id} does not have student {id}.");
            }
        }
    }

    private async Task HandleDeleteInstructor(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("Received an empty delete instructor request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        if (!ObjectId.TryParse(id, out var _id))
        {
            Console.WriteLine("Invalid id");
            return;
        }

        var courses = await repo.GetAllCoursesAsync();

        if (courses == null || courses.Count == 0)
        {
            Console.WriteLine("Can't get courses from repo");
            return;
        }
        var list = courses.FindAll(s => s.Instructors.Contains(id));
        if (list == null || list.Count == 0)
        {
            Console.WriteLine($"Course not found for deleting instructor {id}.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var course = list[i];

            if (course.Instructors.Contains(id))
            {
                ArgumentNullException.ThrowIfNull(course.Id);
                var res = await repo.RemoveInstructorAsync(course.Id, id);

                Console.WriteLine($"Updated course {course.Id} - Success: {res.ModifiedCount > 0}");
                Console.WriteLine($"Deleted instructor {id} from course {course.Id}.");
            }
            else
            {
                Console.WriteLine($"The course {course.Id} does not have instructor {id}.");
            }
        }
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
