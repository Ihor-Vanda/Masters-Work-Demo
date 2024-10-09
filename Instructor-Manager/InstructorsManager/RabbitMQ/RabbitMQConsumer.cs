
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using InstructorsManager.Repository;

namespace InstructorsManager.RabbitMQ;

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

        channel.QueueDeclare(queue: "instructor-course-delete",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        channel.QueueDeclare(queue: "instructor-course-add",
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
                case "instructor-course-delete":
                    await HandleDeleteCourseRequest(message);
                    break;

                case "instructor-course-add":
                    await HandleAddCourseRequest(message);
                    break;
            }
        };

        channel.BasicConsume(queue: "instructor-course-delete",
                             autoAck: true,
                             consumer: consumer);

        channel.BasicConsume(queue: "instructor-course-add",
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task HandleDeleteCourseRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        var parts = message.Split(',');
        if (parts.Length < 2)
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        var courseId = parts[0];
        var instructorIds = parts.ToList();
        instructorIds.RemoveAt(0);

        var instructors = await repo.GetAllInstructors();

        if (instructors != null && instructors.Count > 0)
        {
            var list = instructors.FindAll(s => instructorIds.Contains(s.Id));
            if (list != null)
            {
                var updateTasks = list
                .Where(s => s.Courses.Contains(courseId))
                .Select(async instructor =>
                {
                    instructor.Courses.Remove(courseId);
                    await repo.UpdateInstructor(instructor.Id, instructor);
                    Console.WriteLine($"Deleted course {courseId} from instructor {instructor.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Instructors not found for deleting course {courseId}.");
        }

        Console.WriteLine($"Can't get instructors from repo");
    }


    private async Task HandleAddCourseRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        var parts = message.Split(',');
        if (parts.Length < 2)
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        var courseId = parts[0];
        var instructorIds = parts.ToList();
        instructorIds.RemoveAt(0);

        var instructors = await repo.GetAllInstructors();

        if (instructors != null)
        {
            var list = instructors.FindAll(s => instructorIds.Contains(s.Id));
            if (list != null)
            {
                var updateTasks = list.Select(async instructor =>
                {
                    instructor.Courses.Add(courseId);
                    await repo.UpdateInstructor(instructor.Id, instructor);
                    Console.WriteLine($"Added course {courseId} to instructor {instructor.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Instructors not found for adding course {courseId}.");
        }
        Console.WriteLine($"Can't get Instructors from repo");
    }
}