using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CoursesManager.DTO
{
    public class Course
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Auto-generated by MongoDB

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public string? CourseCode { get; set; }

        public string? Duration { get; set; }

        public string? Language { get; set; }

        public string? Status { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int MaxStudents { get; set; }

        public List<int> Instructors { get; set; } = new List<int>(); // Initialize as empty list

        public List<int> Students { get; set; } = new List<int>(); // Initialize as empty list

        public List<int> Tests { get; set; } = new List<int>(); // Initialize as empty list

        public string? DiscussionForum { get; set; }
    }

}
