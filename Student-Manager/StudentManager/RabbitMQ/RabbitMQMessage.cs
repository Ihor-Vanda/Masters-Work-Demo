namespace StudentManager.RabbitMQ;

public class RabbitMQMessage
{
    public string? Type { get; set; }

    public string? CourseId { get; set; }

    public List<string> EntityIds { get; set; } = [];
}