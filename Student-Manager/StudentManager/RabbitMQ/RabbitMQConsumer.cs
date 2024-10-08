
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StudentManager.Repository;

namespace StudentManager.RabbitMQ;

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
            Port = 5672
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Черга для видалення студентів з курсів
        channel.QueueDeclare(queue: "delete-course-requests",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        // Черга для додавання студентів до курсів
        channel.QueueDeclare(queue: "add-course-requests",
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
                case "delete-course-requests":
                    await HandleDeleteCourseRequest(message);
                    break;

                case "add-course-requests":
                    await HandleAddCourseRequest(message);
                    break;
            }
        };

        channel.BasicConsume(queue: "delete-course-request",
                             autoAck: true,
                             consumer: consumer);

        channel.BasicConsume(queue: "add-course-requests",
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
        var studentRepository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var parts = message.Split(',');
        var courseId = parts[0];
        var studentsId = parts.ToList();
        studentsId.RemoveAt(0);

        var students = await studentRepository.GetAllStudents();

        if (students != null && students.Count > 0)
        {
            var list = students.FindAll(s => studentsId.Contains(s.Id));
            if (list != null)
            {
                var updateTasks = list
                .Where(s => s.Courses.Contains(courseId))
                .Select(async student =>
                {
                    student.Courses.Remove(courseId);
                    await studentRepository.UpdateStudentAsync(student.Id, student);
                    Console.WriteLine($"Deleted course {courseId} from student {student.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Students not found for deleting course {courseId}.");
        }

        Console.WriteLine($"Can't get students from repo");
    }


    private async Task HandleAddCourseRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Received an empty delete course request.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var studentRepository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var parts = message.Split(',');
        var courseId = parts[0];
        var studentsId = parts.ToList();
        studentsId.RemoveAt(0);

        var students = await studentRepository.GetAllStudents();

        if (students != null)
        {
            var list = students.FindAll(s => studentsId.Contains(s.Id));
            if (list != null)
            {
                var updateTasks = list.Select(async student =>
                {
                    student.Courses.Add(courseId);
                    await studentRepository.UpdateStudentAsync(student.Id, student);
                    Console.WriteLine($"Added course {courseId} to student {student.Id}.");
                });

                await Task.WhenAll(updateTasks);
                return;
            }
            Console.WriteLine($"Students not found for adding course {courseId}.");
        }
        Console.WriteLine($"Can't get students from repo");

    }
}