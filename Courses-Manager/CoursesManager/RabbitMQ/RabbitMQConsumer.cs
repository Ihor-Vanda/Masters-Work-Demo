
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CoursesManager.Repository;
using MongoDB.Bson;

namespace CoursesManager.RabbitMQ;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQConsumer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            Port = 5672,
            UserName = "user",
            Password = "password"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "instructor-delete",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        channel.QueueDeclare(queue: "student-delete",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var routingKey = ea.RoutingKey;

            var message = Encoding.UTF8.GetString(body);
            switch (routingKey)
            {
                case "instructor-delete":
                    await HandleDeleteStudent(message);
                    break;

                case "student-delete":
                    await HandleDeleteInstructor(message);
                    break;
            }
        };

        channel.BasicConsume(queue: "instructor-delete",
                             autoAck: true,
                             consumer: consumer);

        channel.BasicConsume(queue: "student-delete",
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task HandleDeleteStudent(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        if (!ObjectId.TryParse(id, out var _id))
        {
            Console.WriteLine("Ivalid id");
            return;
        }

        var courses = await repo.GetAllCoursesAsync();

        if (courses != null && courses.Count > 0)
        {
            var list = courses.FindAll(s => s.Students.Contains(id));
            if (list != null)
            {
                var updateTasks = list
                .Where(s => s.Students.Contains(id))
                .Select(async course =>
                {
                    course.Students.Remove(id);
                    await repo.UpdateCourseAsync(course.Id, course);
                    Console.WriteLine($"Deleted student {id} from course {course.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Coures not found for deleting student {id}.");
        }

        Console.WriteLine($"Can't get courses from repo");
    }


    private async Task HandleDeleteInstructor(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        if (!ObjectId.TryParse(id, out var _id))
        {
            Console.WriteLine("Ivalid id");
            return;
        }

        var courses = await repo.GetAllCoursesAsync();

        if (courses != null && courses.Count > 0)
        {
            var list = courses.FindAll(s => s.Instructors.Contains(id));
            if (list != null)
            {
                var updateTasks = list
                .Where(s => s.Instructors.Contains(id))
                .Select(async course =>
                {
                    course.Instructors.Remove(id);
                    await repo.UpdateCourseAsync(course.Id, course);
                    Console.WriteLine($"Deleted instructor {id} from course {course.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Coures not found for deleting instructor {id}.");
        }

        Console.WriteLine($"Can't get courses from repo");
    }
}