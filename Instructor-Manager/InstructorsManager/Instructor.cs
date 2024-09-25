using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InstructorsManager;

public class Instructor
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    public DateTime BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public List<string> Courses { get; set; } = [];
}
