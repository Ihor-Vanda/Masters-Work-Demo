using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestsManager;

public class Test
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    public string? ReletedCourseId { get; set; }

    [Required]
    public List<Question>? Questions { get; set; } = [];
}